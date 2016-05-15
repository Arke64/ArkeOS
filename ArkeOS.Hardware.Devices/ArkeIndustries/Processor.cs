using System;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
	public class Processor : SystemBusDevice, IProcessor {
		private const int MaxCachedInstuctions = 1024;

		private Instruction[] cachedInstructions;
		private Timer systemTimer;
		private ulong[] registers;
		private ulong cacheBaseAddress;
		private bool supressRIPIncrement;
		private bool interruptsEnabled;
		private bool inIsr;
		private byte protectionMode;
		private byte systemTickInterval;
		private bool instructionCacheEnabled;
		private bool running;

		private Operand operandA;
		private Operand operandB;
		private Operand operandC;

		public Instruction CurrentInstruction { get; private set; }

		public Action<Operand, Operand, Operand> DebugHandler { get; set; }

		public event EventHandler ExecutionBroken;

		public Processor() : base(ProductIds.Vendor, ProductIds.PROC100, DeviceType.Processor) {
			this.operandA = new Operand();
			this.operandB = new Operand();
			this.operandC = new Operand();

			this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
		}

		public override void Start() {
			throw new InvalidOperationException();
		}

		public void Start(ulong bootManagerId) {
			this.cacheBaseAddress = ulong.MaxValue;
			this.supressRIPIncrement = false;
			this.interruptsEnabled = false;
			this.inIsr = false;
			this.running = false;
			this.systemTickInterval = 50;
			this.instructionCacheEnabled = true;
			this.protectionMode = 0;

			this.registers = new ulong[0xFF];

			this.WriteRegister(Register.RF, ulong.MaxValue);
			this.WriteRegister(Register.RIP, bootManagerId << 52);

			this.SetNextInstruction();
		}

		public override void Stop() {
			this.Break();
		}

		public void Break() {
			this.running = false;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.running = true;
			this.systemTimer.Change(this.systemTickInterval, this.systemTickInterval);

			new Task(() => {
				while (this.running)
					this.Tick();
			}, TaskCreationOptions.LongRunning).Start();
		}

		public void Step() {
			this.Tick();
		}

		private void OnSystemTimerTick(object state) {
			if (this.systemTickInterval != 0)
				this.InterruptController.Enqueue(Interrupt.SystemTimer, 0, 0);
		}

		private void SetNextInstruction() {
			var address = this.ReadRegister(Register.RIP);

			if (!this.instructionCacheEnabled) {
				this.CurrentInstruction = new Instruction(this.BusController, address);

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
				this.CurrentInstruction = this.cachedInstructions[offset] = new Instruction(this.BusController, address);
			}
		}

		private void Tick() {
			var execute = true;

			if (this.CurrentInstruction.ConditionalParameter != null) {
				var value = this.GetValue(this.CurrentInstruction.ConditionalParameter);

				execute = (this.CurrentInstruction.ConditionalZero && value == 0) || (!this.CurrentInstruction.ConditionalZero && value != 0);
			}

			if (execute) {
				if (InstructionDefinition.Find(this.CurrentInstruction.Code) != null) { 
					this.LoadParameters(this.operandA, this.operandB, this.operandC);

					this.Execute(this.CurrentInstruction.Code, this.operandA, this.operandB, this.operandC);

					this.SaveParameters(this.operandA, this.operandB, this.operandC);
				}
				else {
					this.InterruptController.Enqueue(Interrupt.InvalidInstruction, this.CurrentInstruction.Code, 0);
				}
			}

			if (!this.supressRIPIncrement) {
				this.WriteRegister(Register.RIP, this.ReadRegister(Register.RIP) + this.CurrentInstruction.Length);
			}
			else {
				this.supressRIPIncrement = false;
			}

			if (this.interruptsEnabled && !this.inIsr && this.InterruptController.PendingCount != 0)
				this.EnterInterrupt(this.InterruptController.Dequeue());

			this.SetNextInstruction();
		}

		private void LoadParameters(Operand a, Operand b, Operand c) {
			c.Reset(this.CurrentInstruction.Definition.ParameterCount >= 3 && (this.CurrentInstruction.Definition.Parameter3Direction & ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter3) : 0);
			b.Reset(this.CurrentInstruction.Definition.ParameterCount >= 2 && (this.CurrentInstruction.Definition.Parameter2Direction & ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter2) : 0);
			a.Reset(this.CurrentInstruction.Definition.ParameterCount >= 1 && (this.CurrentInstruction.Definition.Parameter1Direction & ParameterDirection.Read) != 0 ? this.GetValue(this.CurrentInstruction.Parameter1) : 0);
		}

		private void SaveParameters(Operand a, Operand b, Operand c) {
			if (c.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 3 && (this.CurrentInstruction.Definition.Parameter3Direction & ParameterDirection.Write) != 0)
				this.SetValue(this.CurrentInstruction.Parameter3, c.Value);

			if (b.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 2 && (this.CurrentInstruction.Definition.Parameter2Direction & ParameterDirection.Write) != 0)
				this.SetValue(this.CurrentInstruction.Parameter2, b.Value);

			if (a.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 1 && (this.CurrentInstruction.Definition.Parameter1Direction & ParameterDirection.Write) != 0)
				this.SetValue(this.CurrentInstruction.Parameter1, a.Value);
		}

		private void EnterInterrupt(InterruptRecord interrupt) {
			if (interrupt.Handler == 0)
				return;

			this.WriteRegister(Register.RSIP, this.ReadRegister(Register.RIP));
			this.WriteRegister(Register.RIP, interrupt.Handler);
			this.WriteRegister(Register.RINT1, interrupt.Data1);
			this.WriteRegister(Register.RINT2, interrupt.Data2);

			this.inIsr = true;
		}

		private ulong GetValue(Parameter parameter) {
			var value = ulong.MaxValue;

			if (parameter.Type == ParameterType.Register) {
				value = this.ReadRegister(parameter.Register);
			}
			else if (parameter.Type == ParameterType.Stack) {
				value = this.Pop();
			}
			else if (parameter.Type == ParameterType.Address) {
				value = parameter.Address;
			}
			else if (parameter.Type == ParameterType.Calculated) {
				value = this.GetCalculatedValue(parameter);
			}

			if (parameter.IsRIPRelative)
				value += this.ReadRegister(Register.RIP);

			if (parameter.IsIndirect)
				value = this.BusController.ReadWord(value);

			return value;
		}

		private void SetValue(Parameter parameter, ulong value) {
			if (!parameter.IsIndirect) {
				if (parameter.Type == ParameterType.Register) {
					if (parameter.Register != Register.RO && parameter.Register != Register.RF) {
						this.WriteRegister(parameter.Register, value);

						if (parameter.Register == Register.RIP)
							this.supressRIPIncrement = true;
					}
				}
				else if (parameter.Type == ParameterType.Stack) {
					this.Push(value);
				}
			}
			else {
				var address = ulong.MaxValue;

				if (parameter.Type == ParameterType.Register) {
					address = this.ReadRegister(parameter.Register);
				}
				else if (parameter.Type == ParameterType.Stack) {
					address = this.Pop();
				}
				else if (parameter.Type == ParameterType.Address) {
					address = parameter.Address;
				}
				else if (parameter.Type == ParameterType.Calculated) {
					address = this.GetCalculatedValue(parameter);
				}

				if (parameter.IsRIPRelative)
					address += this.ReadRegister(Register.RIP);

				this.BusController.WriteWord(address, value);
			}
		}

		private ulong GetCalculatedValue(Parameter parameter) {
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

		public void Push(ulong value) {
			this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) - 1);

			this.BusController.WriteWord(this.ReadRegister(Register.RSP), value);
		}

		public ulong Pop() {
			var value = this.BusController.ReadWord(this.ReadRegister(Register.RSP));

			this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) + 1);

			return value;
		}

		public ulong ReadRegister(Register register) => this.registers[(int)register];
		public void WriteRegister(Register register, ulong value) => this.registers[(int)register] = value;

		public override ulong ReadWord(ulong address) {
			if (address == 0) {
				return this.protectionMode;
			}
			else if (address == 1) {
				return this.systemTickInterval;
			}
			else if (address == 2) {
				return this.instructionCacheEnabled ? 1UL : 0UL;
			}
			else {
				return 0;
			}
		}

		public override void WriteWord(ulong address, ulong data) {
			if (address == 0) {
				this.protectionMode = (byte)data;
			}
			else if (address == 1) {
				this.systemTickInterval = (byte)data;
				this.systemTimer.Change(this.systemTickInterval, this.systemTickInterval);
			}
			else if (address == 2) {
				this.instructionCacheEnabled = data != 0;
			}
		}

		public class Operand {
			private ulong value;

			public bool Dirty { get; private set; }

			public ulong Value {
				get {
					return this.value;
				}
				set {
					this.value = value;

					this.Dirty = true;
				}
			}

			public Operand() {
				this.value = 0;
				this.Dirty = false;
			}

			public void Reset(ulong value) {
				this.value = value;
				this.Dirty = false;
			}
		}

		#region Handlers
		private void Execute(byte code, Operand a, Operand b, Operand c) {
			switch (code) {
				case 0: this.ExecuteHLT(a, b, c); break;
				case 1: this.ExecuteNOP(a, b, c); break;
				case 2: this.ExecuteINT(a, b, c); break;
				case 3: this.ExecuteEINT(a, b, c); break;
				case 4: this.ExecuteINTE(a, b, c); break;
				case 5: this.ExecuteINTD(a, b, c); break;
				case 6: this.ExecuteXCHG(a, b, c); break;
				case 7: this.ExecuteCAS(a, b, c); break;
				case 8: this.ExecuteMOV(a, b, c); break;

				case 20: this.ExecuteADD(a, b, c); break;
				case 21: this.ExecuteADDF(a, b, c); break;
				case 22: this.ExecuteSUB(a, b, c); break;
				case 23: this.ExecuteSUBF(a, b, c); break;
				case 24: this.ExecuteDIV(a, b, c); break;
				case 25: this.ExecuteDIVF(a, b, c); break;
				case 26: this.ExecuteMUL(a, b, c); break;
				case 27: this.ExecuteMULF(a, b, c); break;
				case 28: this.ExecuteMOD(a, b, c); break;
				case 29: this.ExecuteMODF(a, b, c); break;

				case 40: this.ExecuteSR(a, b, c); break;
				case 41: this.ExecuteSL(a, b, c); break;
				case 42: this.ExecuteRR(a, b, c); break;
				case 43: this.ExecuteRL(a, b, c); break;
				case 44: this.ExecuteNAND(a, b, c); break;
				case 45: this.ExecuteAND(a, b, c); break;
				case 46: this.ExecuteNOR(a, b, c); break;
				case 47: this.ExecuteOR(a, b, c); break;
				case 48: this.ExecuteNXOR(a, b, c); break;
				case 49: this.ExecuteXOR(a, b, c); break;
				case 50: this.ExecuteNOT(a, b, c); break;
				case 51: this.ExecuteGT(a, b, c); break;
				case 52: this.ExecuteGTE(a, b, c); break;
				case 53: this.ExecuteLT(a, b, c); break;
				case 54: this.ExecuteLTE(a, b, c); break;
				case 55: this.ExecuteEQ(a, b, c); break;
				case 56: this.ExecuteNEQ(a, b, c); break;

				case 60: this.ExecuteDBG(a, b, c); break;
				case 61: this.ExecuteBRK(a, b, c); break;
			}
		}

		#region Basic

		private void ExecuteHLT(Operand a, Operand b, Operand c) {
			this.InterruptController.WaitForInterrupt(500);
			this.supressRIPIncrement = true;
		}

		private void ExecuteNOP(Operand a, Operand b, Operand c) {

		}

		private void ExecuteINT(Operand a, Operand b, Operand c) {
			this.InterruptController.Enqueue((Interrupt)a.Value, b.Value, c.Value);
		}

		private void ExecuteEINT(Operand a, Operand b, Operand c) {
			this.WriteRegister(Register.RIP, this.ReadRegister(Register.RSIP));
			this.WriteRegister(Register.RI0, 0);
			this.WriteRegister(Register.RI1, 0);
			this.WriteRegister(Register.RI2, 0);
			this.WriteRegister(Register.RI3, 0);
			this.WriteRegister(Register.RI4, 0);
			this.WriteRegister(Register.RI5, 0);
			this.WriteRegister(Register.RI6, 0);
			this.WriteRegister(Register.RI7, 0);
			this.WriteRegister(Register.RINT1, 0);
			this.WriteRegister(Register.RINT2, 0);
			this.WriteRegister(Register.RSIP, 0);

			this.inIsr = false;
			this.supressRIPIncrement = true;
		}

		private void ExecuteINTE(Operand a, Operand b, Operand c) {
			this.interruptsEnabled = true;
		}

		private void ExecuteINTD(Operand a, Operand b, Operand c) {
			this.interruptsEnabled = false;
		}

		private void ExecuteXCHG(Operand a, Operand b, Operand c) {
			var t = a.Value;

			a.Value = b.Value;
			b.Value = t;
		}

		private void ExecuteCAS(Operand a, Operand b, Operand c) {
			if (c.Value == b.Value) {
				c.Value = a.Value;
			}
			else {
				b.Value = c.Value;
			}
		}

		private void ExecuteMOV(Operand a, Operand b, Operand c) {
			b.Value = a.Value;
		}

		#endregion

		#region Math

		private void ExecuteADD(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = a.Value + b.Value;
			}
		}

		private void ExecuteADDF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.Int64BitsToDouble((long)a.Value);
			var bb = BitConverter.Int64BitsToDouble((long)b.Value);

			c.Value = (ulong)BitConverter.DoubleToInt64Bits(aa + bb);
		}

		private void ExecuteSUB(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = b.Value - a.Value;
			}
		}

		private void ExecuteSUBF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.Int64BitsToDouble((long)a.Value);
			var bb = BitConverter.Int64BitsToDouble((long)b.Value);

			c.Value = (ulong)BitConverter.DoubleToInt64Bits(bb - aa);
		}

		private void ExecuteDIV(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				c.Value = b.Value / a.Value;
			}
			else {
				this.InterruptController.Enqueue(Interrupt.DivideByZero, 0, 0);
			}
		}

		private void ExecuteDIVF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.Int64BitsToDouble((long)a.Value);
			var bb = BitConverter.Int64BitsToDouble((long)b.Value);

			if (aa != 0.0) {
				c.Value = (ulong)BitConverter.DoubleToInt64Bits(bb / aa);
			}
			else {
				this.InterruptController.Enqueue(Interrupt.DivideByZero, 0, 0);
			}
		}

		private void ExecuteMUL(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = a.Value * b.Value;
			}
		}

		private void ExecuteMULF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.Int64BitsToDouble((long)a.Value);
			var bb = BitConverter.Int64BitsToDouble((long)b.Value);

			c.Value = (ulong)BitConverter.DoubleToInt64Bits(bb * aa);
		}

		private void ExecuteMOD(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				c.Value = b.Value % a.Value;
			}
			else {
				this.InterruptController.Enqueue(Interrupt.DivideByZero, 0, 0);
			}
		}

		private void ExecuteMODF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.Int64BitsToDouble((long)a.Value);
			var bb = BitConverter.Int64BitsToDouble((long)b.Value);

			if (aa != 0.0) {
				c.Value = (ulong)BitConverter.DoubleToInt64Bits(bb % aa);
			}
			else {
				this.InterruptController.Enqueue(Interrupt.DivideByZero, 0, 0);
			}
		}

		#endregion

		#region Logic

		private void ExecuteSR(Operand a, Operand b, Operand c) => c.Value = b.Value >> (byte)a.Value;
		private void ExecuteSL(Operand a, Operand b, Operand c) => c.Value = b.Value << (byte)a.Value;
		private void ExecuteRR(Operand a, Operand b, Operand c) => c.Value = (b.Value >> (byte)a.Value) | (b.Value << (64 - (byte)a.Value));
		private void ExecuteRL(Operand a, Operand b, Operand c) => c.Value = (b.Value << (byte)a.Value) | (b.Value >> (64 - (byte)a.Value));
		private void ExecuteNAND(Operand a, Operand b, Operand c) => c.Value = ~(a.Value & b.Value);
		private void ExecuteAND(Operand a, Operand b, Operand c) => c.Value = a.Value & b.Value;
		private void ExecuteNOR(Operand a, Operand b, Operand c) => c.Value = ~(a.Value | b.Value);
		private void ExecuteOR(Operand a, Operand b, Operand c) => c.Value = a.Value | b.Value;
		private void ExecuteNXOR(Operand a, Operand b, Operand c) => c.Value = ~(a.Value ^ b.Value);
		private void ExecuteXOR(Operand a, Operand b, Operand c) => c.Value = a.Value ^ b.Value;
		private void ExecuteNOT(Operand a, Operand b, Operand c) => b.Value = ~a.Value;
		private void ExecuteGT(Operand a, Operand b, Operand c) => c.Value = b.Value > a.Value ? ulong.MaxValue : 0;
		private void ExecuteGTE(Operand a, Operand b, Operand c) => c.Value = b.Value >= a.Value ? ulong.MaxValue : 0;
		private void ExecuteLT(Operand a, Operand b, Operand c) => c.Value = b.Value < a.Value ? ulong.MaxValue : 0;
		private void ExecuteLTE(Operand a, Operand b, Operand c) => c.Value = b.Value <= a.Value ? ulong.MaxValue : 0;
		private void ExecuteEQ(Operand a, Operand b, Operand c) => c.Value = b.Value == a.Value ? ulong.MaxValue : 0;
		private void ExecuteNEQ(Operand a, Operand b, Operand c) => c.Value = b.Value != a.Value ? ulong.MaxValue : 0;

		#endregion

		#region Debug

		private void ExecuteDBG(Operand a, Operand b, Operand c) {
			this.DebugHandler?.Invoke(a, b, c);
		}

		private void ExecuteBRK(Operand a, Operand b, Operand c) {
			this.supressRIPIncrement = true;

			this.WriteRegister(Register.RIP, this.ReadRegister(Register.RIP) + this.CurrentInstruction.Length);
			this.SetNextInstruction();

			this.Break();

			this.ExecutionBroken?.Invoke(this, EventArgs.Empty);
		}

		#endregion
		#endregion
	}
}