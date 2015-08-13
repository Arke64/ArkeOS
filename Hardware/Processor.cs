using System;
using System.Collections.Generic;
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

		private Dictionary<ulong, Instruction> instructionCache;
		private Configuration configuration;
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Action<Instruction>[] instructionHandlers;
		private Timer systemTimer;
		private bool supressRIPIncrement;
		private bool interruptsEnabled;
		private bool inProtectedIsr;
		private bool inIsr;
		private bool running;

		public Instruction CurrentInstruction { get; private set; }
		public RegisterManager Registers { get; private set; }

		public event EventHandler ExecutionPaused;

		public Processor(MemoryController memoryController, InterruptController interruptController) {
			this.memoryController = memoryController;
			this.interruptController = interruptController;

			this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
			this.instructionHandlers = new Action<Instruction>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Instruction>)this.GetType().GetMethod("Execute" + i.CamelCaseMnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Instruction>), this);
		}

		public void LoadStartupImage(byte[] image) {
			this.memoryController.CopyFrom(image, 0, (ulong)image.Length);
		}

		public void Start() {
			this.running = true;

			this.Reset();

			this.SetNextInstruction();
		}

		public void Stop() {
			this.Break();
		}

		public void Break() {
			this.running = false;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.running = true;
			this.systemTimer.Change(this.configuration.SystemTickInterval, this.configuration.SystemTickInterval);

			new Task(() => {
				while (this.running)
					this.Tick();
			}, TaskCreationOptions.LongRunning).Start();
		}

		public void Step() {
			this.Tick();
		}

		private void Reset() {
			this.Registers = new RegisterManager();

			this.configuration = new Configuration();
			this.instructionCache = new Dictionary<ulong, Instruction>();

			this.supressRIPIncrement = false;
			this.interruptsEnabled = true;
			this.inProtectedIsr = false;
			this.inIsr = false;
		}

		private void OnSystemTimerTick(object state) {
			if (this.configuration.SystemTickInterval != 0)
				this.interruptController.Enqueue(Interrupt.SystemTimer);
		}

		private void SetNextInstruction() {
			var address = this.Registers[Register.RIP];

			Instruction instruction;

			if (!this.configuration.InstructionPreParseEnabled) {
				instruction = this.memoryController.ReadInstruction(address);
			}
			else if (!this.instructionCache.TryGetValue(address, out instruction)) {
				instruction = this.memoryController.ReadInstruction(address);

				this.instructionCache[address] = instruction;
			}

			this.CurrentInstruction = instruction;
		}

		private void Tick() {
			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.instructionHandlers[this.CurrentInstruction.Code](this.CurrentInstruction);

				if (!this.supressRIPIncrement)
					this.Registers[Register.RIP] += this.CurrentInstruction.Length;

				this.supressRIPIncrement = false;
			}
			else {
				this.interruptController.Enqueue(Interrupt.InvalidInstruction);
			}

			if (this.interruptsEnabled && !this.inIsr && this.interruptController.AnyPending)
				this.EnterInterrupt(this.interruptController.Dequeue());

			this.SetNextInstruction();
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

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}