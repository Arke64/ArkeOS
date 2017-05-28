using ArkeOS.Hardware.Architecture;
using System;
using System.Threading.Tasks;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class Processor : SystemBusDevice, IProcessor {
        private ulong[] registers;
        private Instruction[] instructionCache;
        private ulong instructionCacheBaseAddress;
        private ulong instructionCacheSize;
        private ulong executingAddress;
        private byte systemTickInterval;
        private bool interruptsEnabled;
        private bool inIsr;
        private bool running;
        private bool disposed;

        private Operand operandA;
        private Operand operandB;
        private Operand operandC;
        private Task runner;
        private Task systemTimer;

        public Instruction CurrentInstruction { get; private set; }
        public ulong StartAddress { get; set; }
        public Action<Operand, Operand, Operand> DebugHandler { get; set; }
        public Action BreakHandler { get; set; }

        public Processor() : base(ProductIds.Vendor, ProductIds.PROC100, DeviceType.Processor) {
            this.operandA = new Operand();
            this.operandB = new Operand();
            this.operandC = new Operand();
            this.registers = new ulong[32];
            this.disposed = false;
        }

        public override void Reset() {
            if (this.running)
                this.Break();

            Array.Clear(this.registers, 0, this.registers.Length);

            this.executingAddress = this.StartAddress;
            this.instructionCacheBaseAddress = this.StartAddress;
            this.instructionCacheSize = 4096;
            this.instructionCache = new Instruction[this.instructionCacheSize];
            this.interruptsEnabled = false;
            this.inIsr = false;
            this.systemTickInterval = 50;

            this.WriteRegister(Register.RMAX, ulong.MaxValue);
            this.WriteRegister(Register.RONE, 1);
            this.WriteRegister(Register.RZERO, 0);
            this.WriteRegister(Register.RIP, this.StartAddress);

            this.SetNextInstruction();
        }

        public void Break() => this.running = false;

        public void Continue() {
            this.running = true;

            this.runner = new Task(() => {
                while (this.running)
                    this.Tick();
            }, TaskCreationOptions.LongRunning);
            this.runner.Start();

            this.systemTimer = new Task(async () => {
                while (this.running) {
                    await Task.Delay(this.systemTickInterval);

                    this.RaiseInterrupt(Interrupt.SystemTimer, 0, 0);
                }
            }, TaskCreationOptions.LongRunning);
            this.systemTimer.Start();
        }

        public void Step() => this.Tick();

        protected override void Dispose(bool disposing) {
            if (this.disposed)
                return;

            if (disposing) {
                this.Break();

                this.runner?.Wait();
                this.systemTimer?.Wait();

                this.instructionCache = null;
                this.registers = null;
            }

            this.disposed = true;

            base.Dispose(disposing);
        }

        private void SetNextInstruction() {
            if (this.instructionCacheSize == 0) {
                this.CurrentInstruction = new Instruction(this.BusController, this.executingAddress);

                return;
            }

            if (this.executingAddress < this.instructionCacheBaseAddress || this.executingAddress >= this.instructionCacheBaseAddress + this.instructionCacheSize) {
                this.instructionCacheBaseAddress = this.executingAddress;

                Array.Clear(this.instructionCache, 0, (int)this.instructionCacheSize);
            }

            var offset = this.executingAddress - this.instructionCacheBaseAddress;

            if (this.instructionCache[offset] != null) {
                this.CurrentInstruction = this.instructionCache[offset];
            }
            else {
                this.CurrentInstruction = this.instructionCache[offset] = new Instruction(this.BusController, this.executingAddress);
            }
        }

        private void Tick() {
            var execute = true;

            this.WriteRegister(Register.RIP, this.executingAddress + this.CurrentInstruction.Length);

            if (this.CurrentInstruction.ConditionalParameter != null) {
                var value = this.GetValue(this.CurrentInstruction.ConditionalParameter);

                execute = (this.CurrentInstruction.ConditionalZero && value == 0) || (!this.CurrentInstruction.ConditionalZero && value != 0);
            }

            if (execute) {
                if (InstructionDefinition.IsCodeValid(this.CurrentInstruction.Code)) {
                    this.LoadParameters(this.operandA, this.operandB, this.operandC);

                    this.Execute(this.CurrentInstruction.Code, this.operandA, this.operandB, this.operandC);

                    this.SaveParameters(this.operandA, this.operandB, this.operandC);
                }
                else {
                    this.RaiseInterrupt(Interrupt.InvalidInstruction, this.CurrentInstruction.Code, this.executingAddress);
                }
            }

            this.executingAddress = this.ReadRegister(Register.RIP);

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
            if (c.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 3 && (this.CurrentInstruction.Definition.Parameter3Direction & ParameterDirection.Write) != 0) this.SetValue(this.CurrentInstruction.Parameter3, c.Value);
            if (b.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 2 && (this.CurrentInstruction.Definition.Parameter2Direction & ParameterDirection.Write) != 0) this.SetValue(this.CurrentInstruction.Parameter2, b.Value);
            if (a.Dirty && this.CurrentInstruction.Definition.ParameterCount >= 1 && (this.CurrentInstruction.Definition.Parameter1Direction & ParameterDirection.Write) != 0) this.SetValue(this.CurrentInstruction.Parameter1, a.Value);
        }

        private void EnterInterrupt(InterruptRecord interrupt) {
            if (interrupt.Handler == 0)
                return;

            this.WriteRegister(Register.RSIP, this.executingAddress);
            this.WriteRegister(Register.RIP, interrupt.Handler);
            this.WriteRegister(Register.RINT1, interrupt.Data1);
            this.WriteRegister(Register.RINT2, interrupt.Data2);

            this.executingAddress = interrupt.Handler;

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
            else if (parameter.Type == ParameterType.Literal) {
                value = parameter.Literal;
            }
            else if (parameter.Type == ParameterType.Calculated) {
                value = this.GetCalculatedValue(parameter);
            }

            if (parameter.IsRIPRelative)
                value += this.executingAddress;

            if (parameter.IsIndirect)
                value = this.BusController.ReadWord(value);

            return value;
        }

        private void SetValue(Parameter parameter, ulong value) {
            if (!parameter.IsIndirect) {
                if (parameter.Type == ParameterType.Register) {
                    if (parameter.Register != Register.RZERO && parameter.Register != Register.RONE && parameter.Register != Register.RMAX) {
                        this.WriteRegister(parameter.Register, value);
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
                else if (parameter.Type == ParameterType.Literal) {
                    address = parameter.Literal;
                }
                else if (parameter.Type == ParameterType.Calculated) {
                    address = this.GetCalculatedValue(parameter);
                }

                if (parameter.IsRIPRelative)
                    address += this.executingAddress;

                this.BusController.WriteWord(address, value);
            }
        }

        private ulong GetCalculatedValue(Parameter parameter) {
            var address = this.GetValue(parameter.Base.Parameter);

            var index = this.GetValue(parameter.Index.Parameter) * this.GetValue(parameter.Scale.Parameter);
            if ((parameter.Index.IsPositive && parameter.Scale.IsPositive) || (!parameter.Index.IsPositive && !parameter.Scale.IsPositive)) {
                address += index;
            }
            else {
                address -= index;
            }

            var offset = this.GetValue(parameter.Offset.Parameter);
            if (parameter.Offset.IsPositive) {
                address += offset;
            }
            else {
                address -= offset;
            }

            return address;
        }

        public void Push(ulong value) {
            this.BusController.WriteWord(this.ReadRegister(Register.RSP), value);

            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) - 1);
        }

        public ulong Pop() {
            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) + 1);

            return this.BusController.ReadWord(this.ReadRegister(Register.RSP));
        }

        public ulong ReadRegister(Register register) => this.registers[(int)register];
        public void WriteRegister(Register register, ulong value) => this.registers[(int)register] = value;

        public override ulong ReadWord(ulong address) {
            if (address == 0) {
                return this.systemTickInterval;
            }
            else if (address == 1) {
                return this.instructionCacheSize;
            }
            else {
                return 0;
            }
        }

        public override void WriteWord(ulong address, ulong data) {
            if (address == 0) {
                this.systemTickInterval = (byte)data;
            }
            else if (address == 1) {
                this.instructionCacheSize = data;

                if (data > 0)
                    this.instructionCache = new Instruction[data];
            }
        }

        public class Operand {
            private ulong value;

            public bool Dirty { get; private set; }

            public ulong Value {
                get => this.value;
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
                case 8: this.ExecuteSET(a, b, c); break;
                case 9: this.ExecuteCPY(a, b, c); break;
                case 10: this.ExecuteCALL(a, b, c); break;
                case 11: this.ExecuteRET(a, b, c); break;

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
                case 30: this.ExecuteITOF(a, b, c); break;
                case 31: this.ExecuteFTOI(a, b, c); break;

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
            this.InterruptController.WaitForInterrupt(100);

            this.WriteRegister(Register.RIP, this.executingAddress);
        }

        private void ExecuteNOP(Operand a, Operand b, Operand c) {

        }

        private void ExecuteINT(Operand a, Operand b, Operand c) => this.RaiseInterrupt((Interrupt)a.Value, b.Value, c.Value);

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
        }

        private void ExecuteINTE(Operand a, Operand b, Operand c) => this.interruptsEnabled = true;

        private void ExecuteINTD(Operand a, Operand b, Operand c) => this.interruptsEnabled = false;

        private void ExecuteXCHG(Operand a, Operand b, Operand c) {
            var t = a.Value;

            a.Value = b.Value;
            b.Value = t;
        }

        private void ExecuteCAS(Operand a, Operand b, Operand c) {
            if (a.Value == b.Value) {
                a.Value = c.Value;
            }
            else {
                b.Value = a.Value;
            }
        }

        private void ExecuteSET(Operand a, Operand b, Operand c) => a.Value = b.Value;

        private void ExecuteCPY(Operand a, Operand b, Operand c) {
            this.BusController.Copy(b.Value, a.Value, c.Value);
            this.RaiseInterrupt(Interrupt.CPYComplete, b.Value, a.Value);
        }

        private void ExecuteCALL(Operand a, Operand b, Operand c) {
            this.Push(this.ReadRegister(Register.RIP));
            this.WriteRegister(Register.RIP, a.Value);
        }

        private void ExecuteRET(Operand a, Operand b, Operand c) => this.WriteRegister(Register.RIP, this.Pop());

        #endregion

        #region Math

        private void ExecuteADD(Operand a, Operand b, Operand c) {
            unchecked {
                a.Value = b.Value + c.Value;
            }
        }

        private void ExecuteADDF(Operand a, Operand b, Operand c) {
            var bb = BitConverter.Int64BitsToDouble((long)b.Value);
            var cc = BitConverter.Int64BitsToDouble((long)c.Value);

            a.Value = (ulong)BitConverter.DoubleToInt64Bits(bb + cc);
        }

        private void ExecuteSUB(Operand a, Operand b, Operand c) {
            unchecked {
                a.Value = b.Value - c.Value;
            }
        }

        private void ExecuteSUBF(Operand a, Operand b, Operand c) {
            var bb = BitConverter.Int64BitsToDouble((long)b.Value);
            var cc = BitConverter.Int64BitsToDouble((long)c.Value);

            a.Value = (ulong)BitConverter.DoubleToInt64Bits(bb - cc);
        }

        private void ExecuteDIV(Operand a, Operand b, Operand c) {
            if (c.Value != 0) {
                a.Value = b.Value / c.Value;
            }
            else {
                this.RaiseInterrupt(Interrupt.DivideByZero, this.executingAddress, 0);
            }
        }

        private void ExecuteDIVF(Operand a, Operand b, Operand c) {
            var bb = BitConverter.Int64BitsToDouble((long)b.Value);
            var cc = BitConverter.Int64BitsToDouble((long)c.Value);

            if (bb != 0.0) {
                a.Value = (ulong)BitConverter.DoubleToInt64Bits(bb / cc);
            }
            else {
                this.RaiseInterrupt(Interrupt.DivideByZero, this.executingAddress, 0);
            }
        }

        private void ExecuteMUL(Operand a, Operand b, Operand c) {
            unchecked {
                a.Value = b.Value * c.Value;
            }
        }

        private void ExecuteMULF(Operand a, Operand b, Operand c) {
            var bb = BitConverter.Int64BitsToDouble((long)b.Value);
            var cc = BitConverter.Int64BitsToDouble((long)c.Value);

            a.Value = (ulong)BitConverter.DoubleToInt64Bits(bb * cc);
        }

        private void ExecuteMOD(Operand a, Operand b, Operand c) {
            if (c.Value != 0) {
                a.Value = b.Value % c.Value;
            }
            else {
                this.RaiseInterrupt(Interrupt.DivideByZero, this.executingAddress, 0);
            }
        }

        private void ExecuteMODF(Operand a, Operand b, Operand c) {
            var bb = BitConverter.Int64BitsToDouble((long)b.Value);
            var cc = BitConverter.Int64BitsToDouble((long)c.Value);

            if (bb != 0.0) {
                a.Value = (ulong)BitConverter.DoubleToInt64Bits(bb % cc);
            }
            else {
                this.RaiseInterrupt(Interrupt.DivideByZero, this.executingAddress, 0);
            }
        }

        private void ExecuteITOF(Operand a, Operand b, Operand c) => a.Value = (ulong)BitConverter.DoubleToInt64Bits(b.Value);

        private void ExecuteFTOI(Operand a, Operand b, Operand c) => a.Value = (ulong)BitConverter.Int64BitsToDouble((long)b.Value);

        #endregion

        #region Logic

        private void ExecuteSR(Operand a, Operand b, Operand c) => a.Value = b.Value >> (byte)c.Value;
        private void ExecuteSL(Operand a, Operand b, Operand c) => a.Value = b.Value << (byte)c.Value;
        private void ExecuteRR(Operand a, Operand b, Operand c) => a.Value = (b.Value >> (byte)c.Value) | (b.Value << (64 - (byte)c.Value));
        private void ExecuteRL(Operand a, Operand b, Operand c) => a.Value = (b.Value << (byte)c.Value) | (b.Value >> (64 - (byte)c.Value));
        private void ExecuteNAND(Operand a, Operand b, Operand c) => a.Value = ~(b.Value & c.Value);
        private void ExecuteAND(Operand a, Operand b, Operand c) => a.Value = (b.Value & c.Value);
        private void ExecuteNOR(Operand a, Operand b, Operand c) => a.Value = ~(b.Value | c.Value);
        private void ExecuteOR(Operand a, Operand b, Operand c) => a.Value = (b.Value | c.Value);
        private void ExecuteNXOR(Operand a, Operand b, Operand c) => a.Value = ~(b.Value ^ c.Value);
        private void ExecuteXOR(Operand a, Operand b, Operand c) => a.Value = (b.Value ^ c.Value);
        private void ExecuteNOT(Operand a, Operand b, Operand c) => a.Value = ~b.Value;
        private void ExecuteGT(Operand a, Operand b, Operand c) => a.Value = b.Value > c.Value ? ulong.MaxValue : 0;
        private void ExecuteGTE(Operand a, Operand b, Operand c) => a.Value = b.Value >= c.Value ? ulong.MaxValue : 0;
        private void ExecuteLT(Operand a, Operand b, Operand c) => a.Value = b.Value < c.Value ? ulong.MaxValue : 0;
        private void ExecuteLTE(Operand a, Operand b, Operand c) => a.Value = b.Value <= c.Value ? ulong.MaxValue : 0;
        private void ExecuteEQ(Operand a, Operand b, Operand c) => a.Value = b.Value == c.Value ? ulong.MaxValue : 0;
        private void ExecuteNEQ(Operand a, Operand b, Operand c) => a.Value = b.Value != c.Value ? ulong.MaxValue : 0;

        #endregion

        #region Debug

        private void ExecuteDBG(Operand a, Operand b, Operand c) => this.DebugHandler?.Invoke(a, b, c);

        private void ExecuteBRK(Operand a, Operand b, Operand c) {
            this.executingAddress = this.ReadRegister(Register.RIP);

            this.SetNextInstruction();

            this.Break();

            this.BreakHandler?.Invoke();
        }

        #endregion
        #endregion
    }
}
