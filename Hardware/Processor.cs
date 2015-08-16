using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private static int MaxCachedInstuctions => 1024;

		private class Configuration {
			public ushort SystemTickInterval { get; set; } = 50;
			public bool InstructionCachingEnabled { get; set; } = true;
			public byte ProtectionMode { get; set; } = 0;
		}


		private Configuration configuration;
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Action<Instruction>[] instructionHandlers;
		private Timer systemTimer;
		private Instruction[] cachedInstructions;
		private ulong cacheBaseAddress;
		private bool supressRIPIncrement;
		private bool interruptsEnabled;
		private bool inProtectedIsr;
		private bool inIsr;
		private bool broken;

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
			this.broken = true;

			this.Reset();

			this.SetNextInstruction();
		}

		public void Stop() {
			this.Break();
		}

		public void Break() {
			this.broken = true;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.broken = false;
			this.systemTimer.Change(this.configuration.SystemTickInterval, this.configuration.SystemTickInterval);

			new Task(() => {
				while (!this.broken)
					this.Tick();
			}, TaskCreationOptions.LongRunning).Start();
		}

		public void Step() {
			this.Tick();
		}

		private void Reset() {
			this.Registers = new RegisterManager();

			this.configuration = new Configuration();
			this.cacheBaseAddress = ulong.MaxValue;

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

			if (!this.configuration.InstructionCachingEnabled) {
				this.CurrentInstruction = this.memoryController.ReadInstruction(address);

				return;
			}

			if (address < this.cacheBaseAddress || address >= this.cacheBaseAddress + (ulong)Processor.MaxCachedInstuctions) {
				this.cacheBaseAddress = address;
				this.cachedInstructions = new Instruction[Processor.MaxCachedInstuctions];
			}

			var offset = address - this.cacheBaseAddress;

			if (this.cachedInstructions[offset] != null) {
				this.CurrentInstruction = this.cachedInstructions[offset];
			}
			else {
				this.CurrentInstruction = this.cachedInstructions[offset] = this.memoryController.ReadInstruction(address);
			}

		}

		private void Tick() {
			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.instructionHandlers[this.CurrentInstruction.Code](this.CurrentInstruction);

				if (!this.supressRIPIncrement) {
					this.Registers[Register.RIP] += this.CurrentInstruction.Length;
				}
				else {
					this.supressRIPIncrement = false;
				}
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
			this.configuration.InstructionCachingEnabled = this.memoryController.ReadU16(this.Registers[Register.RCFG] + 2) != 0;
			this.configuration.ProtectionMode = this.memoryController.ReadU8(this.Registers[Register.RCFG] + 3);
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			if (parameter.Type == ParameterType.Literal) {
				value = parameter.Literal;
			}
			else if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.Registers[parameter.Register];
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				value = this.memoryController.ReadU64(parameter.Literal);
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.memoryController.ReadU64(this.Registers[parameter.Register]);
			}

			return value & this.CurrentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterWriteAllowed(parameter.Register)) {
					this.Registers[parameter.Register] = value;

					if (parameter.Register == Register.RCFG)
						this.UpdateConfiguration();
				}
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				this.memoryController.WriteU64(parameter.Literal, value);
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					this.memoryController.WriteU64(this.Registers[parameter.Register], value);
			}
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);

		private void Access(Instruction instruction, Action<ulong> operation) => operation(this.GetValue(instruction.Parameter1));
		private void Access(Instruction instruction, Action<ulong, ulong> operation) => operation(this.GetValue(instruction.Parameter1), this.GetValue(instruction.Parameter2));
		private void Access(Instruction instruction, Func<ulong> operation) => this.SetValue(instruction.Parameter1, operation());
		private void Access(Instruction instruction, Func<ulong, ulong> operation) => this.SetValue(instruction.Parameter2, operation(this.GetValue(instruction.Parameter1)));
		private void Access(Instruction instruction, Func<ulong, ulong, ulong> operation) => this.SetValue(instruction.Parameter3, operation(this.GetValue(instruction.Parameter1), this.GetValue(instruction.Parameter2)));
	}
}