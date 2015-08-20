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
			public ulong SystemTickInterval { get; set; } = 50;
			public bool InstructionCachingEnabled { get; set; } = true;
			public ulong ProtectionMode { get; set; } = 0;
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
			var newImage = new ulong[image.Length / 8];

			Buffer.BlockCopy(image, 0, newImage, 0, image.Length);

			this.memoryController.CopyFrom(newImage, 0, (ulong)newImage.Length);
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
			this.systemTimer.Change((int)this.configuration.SystemTickInterval, (int)this.configuration.SystemTickInterval);

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
			Operand a = new Operand(), b = new Operand(), c = new Operand();

			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.LoadParameters(a, b, c);

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

		private void LoadParameters(Operand a, Operand b, Operand c) {
			if (this.CurrentInstruction.Definition.ParameterCount >= 3 && this.CurrentInstruction.Definition.Parameter3Direction == InstructionDefinition.ParameterDirection.Read)
				c.Reset(this.GetValue(this.CurrentInstruction.Parameter3));

			if (this.CurrentInstruction.Definition.ParameterCount >= 2 && this.CurrentInstruction.Definition.Parameter2Direction == InstructionDefinition.ParameterDirection.Read)
				b.Reset(this.GetValue(this.CurrentInstruction.Parameter2));

			if (this.CurrentInstruction.Definition.ParameterCount >= 1 && this.CurrentInstruction.Definition.Parameter1Direction == InstructionDefinition.ParameterDirection.Read)
				a.Reset(this.GetValue(this.CurrentInstruction.Parameter1));
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

			var isr = this.memoryController.ReadWord(this.Registers[Register.RIDT] + (ulong)id);

			if (isr == 0)
				return;

			this.Registers[Register.RSIP] = this.Registers[Register.RIP];
			this.Registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
			this.inIsr = true;
		}

		private void UpdateConfiguration() {
			this.configuration.SystemTickInterval = this.memoryController.ReadWord(this.Registers[Register.RCFG] + 0);
			this.configuration.InstructionCachingEnabled = this.memoryController.ReadWord(this.Registers[Register.RCFG] + 1) != 0;
			this.configuration.ProtectionMode = this.memoryController.ReadWord(this.Registers[Register.RCFG] + 2);
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			if (parameter.Type == ParameterType.Literal) {
				value = parameter.Literal;
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				value = this.memoryController.ReadWord(parameter.Literal);
			}
			else if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.Registers[parameter.Register];
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.memoryController.ReadWord(this.Registers[parameter.Register]);
			}
			else if (parameter.Type == ParameterType.Calculated) {
				value = this.GetCalculatedLiteral(parameter);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				value = this.memoryController.ReadWord(this.GetCalculatedLiteral(parameter));
			}
			else if (parameter.Type == ParameterType.Stack) {
				value = this.Pop();
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				value = this.memoryController.ReadWord(this.Pop());
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
					this.memoryController.WriteWord(this.Registers[parameter.Register], value);
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				this.memoryController.WriteWord(parameter.Literal, value);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				this.memoryController.WriteWord(this.GetCalculatedLiteral(parameter), value);
			}
			else if (parameter.Type == ParameterType.Stack) {
				this.Push(value);
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				this.memoryController.WriteWord(this.Pop(), value);
			}
		}

		private ulong GetCalculatedLiteral(Parameter parameter) {
			var address = this.GetValue(parameter.Base.Parameter);

			if (parameter.Index != null) {
				var calc = this.GetValue(parameter.Index.Parameter) * (parameter.Scale != null ? this.GetValue(parameter.Scale.Parameter) : 1);

				if (parameter.Index.IsPositive) {
					address += calc;
				}
				else {
					address -= calc;
				}
			}

			if (parameter.Offset != null) {
				var calc = this.GetValue(parameter.Offset.Parameter);

				if (parameter.Offset.IsPositive) {
					address += calc;
				}
				else {
					address -= calc;
				}
			}

			return address;
		}

		private void Push(ulong value) {
			this.Registers[Register.RSP] -= 8;

			this.memoryController.WriteWord(this.Registers[Register.RSP], value);
		}

		private ulong Pop() {
			var value = this.memoryController.ReadWord(this.Registers[Register.RSP]);

			this.Registers[Register.RSP] += 8;

			return value;
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);
	}
}