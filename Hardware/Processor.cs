using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private class Configuration {
			public ushort SystemTickInterval { get; set; } = 50;
			public bool InstructionPreParseEnabled { get; set; } = true;
			public byte ProtectionMode { get; set; } = 0;
		}

		private Tuple<ulong, Instruction>[] instructionCache;
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Action<Instruction>[] instructionHandlers;
		private AutoResetEvent breakEvent;
		private AutoResetEvent stepEvent;
		private Timer systemTimer;
		private bool supressRIPIncrement;
		private bool inProtectedIsr;
		private bool inIsr;
		private bool running;
		private bool broken;
		private bool interruptsEnabled;
		private Configuration configuration;

		public Instruction CurrentInstruction { get; private set; }
		public RegisterManager Registers { get; }

		public event EventHandler ExecutionPaused;

		public Processor(MemoryController memoryController, InterruptController interruptController) {
			this.memoryController = memoryController;
			this.interruptController = interruptController;

			this.configuration = new Configuration();
			this.instructionCache = new Tuple<ulong, Instruction>[64];
			this.breakEvent = new AutoResetEvent(false);
			this.stepEvent = new AutoResetEvent(false);
			this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
			this.instructionHandlers = new Action<Instruction>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Instruction>)this.GetType().GetMethod("Execute" + i.CamelCaseMnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Instruction>), this);

			this.Registers = new RegisterManager();
		}

		public void LoadStartupImage(byte[] image) {
			this.memoryController.CopyFrom(image, 0, (ulong)image.Length);
		}

		public void Start() {
			this.supressRIPIncrement = false;
			this.inProtectedIsr = false;
			this.running = true;
			this.broken = true;
			this.interruptsEnabled = true;

			this.BuildCache(0);

			new Task(() => {
				while (this.running)
					this.Tick();
			}, TaskCreationOptions.LongRunning).Start();
		}

		public void Stop() {
			this.running = false;
			this.broken = false;
			this.breakEvent.Set();
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Break() {
			this.stepEvent.Reset();
			this.broken = true;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.broken = false;
			this.breakEvent.Set();
			this.systemTimer.Change(this.configuration.SystemTickInterval, this.configuration.SystemTickInterval);
		}

		public void Step() {
			this.breakEvent.Set();
			this.stepEvent.WaitOne();
		}

		private void OnSystemTimerTick(object state) {
			if (this.configuration.SystemTickInterval != 0)
				this.interruptController.Enqueue(Interrupt.SystemTimer);
		}

		private Instruction GetNextInstruction() {
			var address = this.Registers[Register.RIP];

			if (!this.configuration.InstructionPreParseEnabled)
				return this.memoryController.ReadInstruction(address);

			foreach (var e in this.instructionCache)
				if (e?.Item1 == address)
					return e.Item2;

			this.BuildCache(address);

			return this.instructionCache[0].Item2;
		}

		private void BuildCache(ulong address) {
			for (var i = 0; i < this.instructionCache.Length; i++) {
				var instruction = this.memoryController.ReadInstruction(address);

				this.instructionCache[i] = Tuple.Create(address, instruction);

				address += instruction.Length;
			}
		}

		private void Tick() {
			if (this.interruptsEnabled && !this.inIsr && this.interruptController.AnyPending)
				this.EnterInterrupt(this.interruptController.Dequeue());

			this.CurrentInstruction = this.GetNextInstruction();

			if (this.broken) {
				this.stepEvent.Set();
				this.breakEvent.WaitOne();
			}

			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.instructionHandlers[this.CurrentInstruction.Code](this.CurrentInstruction);

				if (!this.supressRIPIncrement)
					this.Registers[Register.RIP] += this.CurrentInstruction.Length;

				this.supressRIPIncrement = false;
			}
			else {
				this.interruptController.Enqueue(Interrupt.InvalidInstruction);
			}
		}

		private void EnterInterrupt(Interrupt id) {
			if (this.Registers[Register.RIDT] == 0)
				return;

			var isr = this.memoryController.ReadU64(this.Registers[Register.RIDT] + (ulong)id * 8);

			if (isr == 0)
				return;

			this.Registers[Register.RSIP] = this.Registers[Register.RIP];
			this.Registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
			this.inIsr = true;
		}

		private void UpdateFlags(ulong value) {
			this.Registers[Register.RZ] = value == 0 ? ulong.MaxValue : 0;
			this.Registers[Register.RS] = (value & (ulong)(1 << (this.CurrentInstruction.SizeInBits - 1))) != 0 ? ulong.MaxValue : 0;
		}

		private void UpdateConfiguration() {
			this.configuration.SystemTickInterval = this.memoryController.ReadU16(this.Registers[Register.RCFG] + 0);
			this.configuration.InstructionPreParseEnabled = this.memoryController.ReadU16(this.Registers[Register.RCFG] + 2) != 0;
			this.configuration.ProtectionMode = this.memoryController.ReadU8(this.Registers[Register.RCFG] + 3);
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			switch (parameter.Type) {
				case ParameterType.Literal:
					value = parameter.Literal;
					break;

				case ParameterType.Register:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.Registers[parameter.Register];

					break;

				case ParameterType.LiteralAddress:
					value = this.memoryController.ReadU64(parameter.Literal);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.memoryController.ReadU64(this.Registers[parameter.Register]);

					break;
			}

			return value & this.CurrentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.UpdateFlags(value);

			var orig = value;

			value &= this.CurrentInstruction.SizeMask;

			if (value != orig)
				this.Registers[Register.RC] = ulong.MaxValue;

			switch (parameter.Type) {
				case ParameterType.Register:
					if (this.IsRegisterWriteAllowed(parameter.Register)) {
						this.Registers[parameter.Register] = value;

						if (parameter.Register == Register.RCFG)
							this.UpdateConfiguration();
					}

					break;

				case ParameterType.LiteralAddress:
					this.memoryController.WriteU64(parameter.Literal, value);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						this.memoryController.WriteU64(this.Registers[parameter.Register], value);

					break;
			}
		}

		private void Push(InstructionSize size, ulong value) {
			this.Registers[Register.RSP] -= Helpers.SizeToBytes(size);

			switch (size) {
				case InstructionSize.OneByte: this.memoryController.WriteU8(this.Registers[Register.RSP], (byte)value); break;
				case InstructionSize.TwoByte: this.memoryController.WriteU16(this.Registers[Register.RSP], (ushort)value); break;
				case InstructionSize.FourByte: this.memoryController.WriteU32(this.Registers[Register.RSP], (uint)value); break;
				case InstructionSize.EightByte: this.memoryController.WriteU64(this.Registers[Register.RSP], value); break;
			}
		}

		private ulong Pop(InstructionSize size) {
			var value = 0UL;

			switch (size) {
				case InstructionSize.OneByte: value = this.memoryController.ReadU8(this.Registers[Register.RSP]); break;
				case InstructionSize.TwoByte: value = this.memoryController.ReadU16(this.Registers[Register.RSP]); break;
				case InstructionSize.FourByte: value = this.memoryController.ReadU32(this.Registers[Register.RSP]); break;
				case InstructionSize.EightByte: value = this.memoryController.ReadU64(this.Registers[Register.RSP]); break;
			}

			this.Registers[Register.RSP] += Helpers.SizeToBytes(size);

			return value;
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.ReadProtectedRegisters.Contains(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.WriteProtectedRegisters.Contains(register);

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}