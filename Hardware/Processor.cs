using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private static int MaxCachedInstuctions => 1024;

		private ConfigurationManager configurationManager;
		private SystemBusController systemBusController;
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

		private Operand operandA;
		private Operand operandB;
		private Operand operandC;

		public Instruction CurrentInstruction { get; private set; }
		public RegisterManager Registers { get; private set; }

		public event EventHandler ExecutionPaused;

		public Processor(SystemBusController systemBusController) {
			this.configurationManager = new ConfigurationManager();
			this.interruptController = new InterruptController();

			this.systemBusController = systemBusController;
			this.systemBusController.AddDevice(1, this.configurationManager);

			this.operandA = new Operand();
			this.operandB = new Operand();
			this.operandC = new Operand();

			this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
			this.instructionHandlers = new Action<Operand, Operand, Operand>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Operand, Operand, Operand>)this.GetType().GetMethod("Execute" + i.Mnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Operand, Operand, Operand>), this);
		}

		public void LoadStartupImage(byte[] image) {
			var newImage = new ulong[image.Length / 8];

			Buffer.BlockCopy(image, 0, newImage, 0, image.Length);

			this.systemBusController.CopyFrom(newImage, 0, (ulong)newImage.Length);
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
			this.systemTimer.Change(this.configurationManager.SystemTickInterval, this.configurationManager.SystemTickInterval);

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

			this.configurationManager.Reset();

			this.cacheBaseAddress = ulong.MaxValue;
			this.supressRIPIncrement = false;
			this.interruptsEnabled = true;
			this.inProtectedIsr = false;
			this.inIsr = false;
		}

		private void OnSystemTimerTick(object state) {
			if (this.configurationManager.SystemTickInterval != 0)
				this.interruptController.Enqueue(Interrupt.SystemTimer);
		}

		private void SetNextInstruction() {
			var address = this.Registers[Register.RIP];

			if (!this.configurationManager.InstructionCachingEnabled) {
				this.CurrentInstruction = new Instruction(this.systemBusController, address);

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
				this.CurrentInstruction = this.cachedInstructions[offset] = new Instruction(this.systemBusController, address);
			}
		}

		private void Tick() {
			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.LoadParameters(this.operandA, this.operandB, this.operandC);

				this.instructionHandlers[this.CurrentInstruction.Code](this.operandA, this.operandB, this.operandC);

				this.SaveParameters(this.operandA, this.operandB, this.operandC);

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
			c.Reset(this.CurrentInstruction.Definition.ParameterCount >= 3 && (this.CurrentInstruction.Definition.Parameter3Direction & InstructionDefinition.ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter3) : 0);
			b.Reset(this.CurrentInstruction.Definition.ParameterCount >= 2 && (this.CurrentInstruction.Definition.Parameter2Direction & InstructionDefinition.ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter2) : 0);
			a.Reset(this.CurrentInstruction.Definition.ParameterCount >= 1 && (this.CurrentInstruction.Definition.Parameter1Direction & InstructionDefinition.ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter1) : 0);
		}

		private void SaveParameters(Operand a, Operand b, Operand c) {
			if (this.CurrentInstruction.Definition.ParameterCount >= 3 && (this.CurrentInstruction.Definition.Parameter3Direction & InstructionDefinition.ParameterDirection.Write) != 0 && c.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter3, c.Value);

			if (this.CurrentInstruction.Definition.ParameterCount >= 2 && (this.CurrentInstruction.Definition.Parameter2Direction & InstructionDefinition.ParameterDirection.Write) != 0 && b.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter2, b.Value);

			if (this.CurrentInstruction.Definition.ParameterCount >= 1 && (this.CurrentInstruction.Definition.Parameter1Direction & InstructionDefinition.ParameterDirection.Write) != 0 && a.Dirty)
				this.SetValue(this.CurrentInstruction.Parameter1, a.Value);
		}

		private void EnterInterrupt(Interrupt id) {
			var isr = this.configurationManager.InterruptVectors[(int)id];

			if (isr == 0)
				return;

			this.Registers[Register.RSIP] = this.Registers[Register.RIP];
			this.Registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
			this.inIsr = true;
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.Registers[parameter.Register];
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.systemBusController.ReadWord(this.Registers[parameter.Register]);
			}
			else if (parameter.Type == ParameterType.Stack) {
				value = this.Pop();
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				value = this.systemBusController.ReadWord(this.Pop());
			}
			else if (parameter.Type == ParameterType.Literal) {
				value = parameter.Literal;
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				value = this.systemBusController.ReadWord(parameter.Literal);
			}

			else if (parameter.Type == ParameterType.Calculated) {
				value = this.GetCalculatedLiteral(parameter);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				value = this.systemBusController.ReadWord(this.GetCalculatedLiteral(parameter));
			}

			return value;
		}

		private void SetValue(Parameter parameter, ulong value) {
			if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterWriteAllowed(parameter.Register)) {
					this.Registers[parameter.Register] = value;

					if (parameter.Register == Register.RIP)
						this.supressRIPIncrement = true;
				}
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					this.systemBusController.WriteWord(this.Registers[parameter.Register], value);
			}
			else if (parameter.Type == ParameterType.Stack) {
				this.Push(value);
			}
			else if (parameter.Type == ParameterType.StackAddress) {
				this.systemBusController.WriteWord(this.Pop(), value);
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				this.systemBusController.WriteWord(parameter.Literal, value);
			}
			else if (parameter.Type == ParameterType.CalculatedAddress) {
				this.systemBusController.WriteWord(this.GetCalculatedLiteral(parameter), value);
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

			this.systemBusController.WriteWord(this.Registers[Register.RSP], value);
		}

		private ulong Pop() {
			var value = this.systemBusController.ReadWord(this.Registers[Register.RSP]);

			this.Registers[Register.RSP] += 8;

			return value;
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configurationManager.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configurationManager.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);
	}
}