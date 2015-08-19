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
		private Action<Operand, Operand, Operand>[] instructionHandlers;
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
			this.instructionHandlers = new Action<Operand, Operand, Operand>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Operand, Operand, Operand>)this.GetType().GetMethod("Execute" + i.Mnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Operand, Operand, Operand>), this);
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
			Operand a, b, c;

			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.LoadParameters(out a, out b, out c);

				this.instructionHandlers[this.CurrentInstruction.Code](a, b, c);

				this.SaveParameters(a, b, c);

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

		private void LoadParameters(out Operand a, out Operand b, out Operand c) {
			c = (this.CurrentInstruction.Definition.ParameterCount >= 3 && this.CurrentInstruction.Definition.Parameter3Direction == InstructionDefinition.ParameterDirection.Read) ? new Operand(this.GetValue(this.CurrentInstruction.Parameter3)) : new Operand(0);
			b = (this.CurrentInstruction.Definition.ParameterCount >= 2 && this.CurrentInstruction.Definition.Parameter2Direction == InstructionDefinition.ParameterDirection.Read) ? new Operand(this.GetValue(this.CurrentInstruction.Parameter2)) : new Operand(0);
			a = (this.CurrentInstruction.Definition.ParameterCount >= 1 && this.CurrentInstruction.Definition.Parameter1Direction == InstructionDefinition.ParameterDirection.Read) ? new Operand(this.GetValue(this.CurrentInstruction.Parameter1)) : new Operand(0);
		}

		private void SaveParameters(Operand a, Operand b, Operand c) {
			if (this.CurrentInstruction.Definition.ParameterCount >= 3 && this.CurrentInstruction.Definition.Parameter3Direction == InstructionDefinition.ParameterDirection.Write && c.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter3, c.Value);

			if (this.CurrentInstruction.Definition.ParameterCount >= 2 && this.CurrentInstruction.Definition.Parameter2Direction == InstructionDefinition.ParameterDirection.Write && b.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter2, b.Value);

			if (this.CurrentInstruction.Definition.ParameterCount >= 1 && this.CurrentInstruction.Definition.Parameter1Direction == InstructionDefinition.ParameterDirection.Write && a.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter1, a.Value);
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
			else if (parameter.Type == ParameterType.LiteralAddress) {
				value = this.memoryController.ReadU64(parameter.Literal);
			}
			else if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.Registers[parameter.Register];
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.memoryController.ReadU64(this.Registers[parameter.Register]);
			}
			else if (parameter.Type == ParameterType.CalculatedLiteral) {
				value = this.GetCalculatedLiteral(parameter);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				value = this.memoryController.ReadU64(this.GetCalculatedLiteral(parameter));
			}
			else if (parameter.Type == ParameterType.StackLiteral) {
				value = this.Pop();
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				value = this.memoryController.ReadU64(this.Pop());
			}

			return value;
		}

		private void SetValue(Parameter parameter, ulong value) {
			if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterWriteAllowed(parameter.Register)) {
					this.Registers[parameter.Register] = value;

					if (parameter.Register == Register.RCFG) {
						this.UpdateConfiguration();
					}
					else if (parameter.Register == Register.RIP) {
						this.supressRIPIncrement = true;
					}
				}
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					this.memoryController.WriteU64(this.Registers[parameter.Register], value);
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				this.memoryController.WriteU64(parameter.Literal, value);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				this.memoryController.WriteU64(this.GetCalculatedLiteral(parameter), value);
			}
			else if (parameter.Type == ParameterType.StackLiteral) {
				this.Push(value);
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				this.memoryController.WriteU64(this.Pop(), value);
			}
		}

		private ulong GetCalculatedLiteral(Parameter parameter) {
			var address = this.GetValue(parameter.CalculatedBase);

			if (parameter.CalculatedIndex != null) {
				var calc = this.GetValue(parameter.CalculatedIndex) * (parameter.CalculatedScale != null ? this.GetValue(parameter.CalculatedScale) : 1);

				if (parameter.CalculatedIndexSign) {
					address += calc;
				}
				else {
					address -= calc;
				}
			}

			if (parameter.CalculatedOffset != null)
				address += this.GetValue(parameter.CalculatedOffset);

			return address;
		}

		private void Push(ulong value) {
			this.Registers[Register.RSP] -= 8;

			this.memoryController.WriteU64(this.Registers[Register.RSP], value);
		}

		private ulong Pop() {
			var value = this.memoryController.ReadU64(this.Registers[Register.RSP]);

			this.Registers[Register.RSP] += 8;

			return value;
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);
	}
}