using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public partial class Processor : SystemBusDevice {
        private static int MaxCachedInstuctions => 1024;

        private Action<Operand, Operand, Operand>[] instructionHandlers;
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

        public InterruptController InterruptController { get; set; }

        public event EventHandler ExecutionBroken;

        public Processor() : base(1, 2, DeviceType.Processor) {
            this.operandA = new Operand();
            this.operandB = new Operand();
            this.operandC = new Operand();

            this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            this.instructionHandlers = new Action<Operand, Operand, Operand>[InstructionDefinition.All.Max(i => i.Code) + 1];

            foreach (var i in InstructionDefinition.All)
                this.instructionHandlers[i.Code] = (Action<Operand, Operand, Operand>)this.GetType().GetMethod("Execute" + i.Mnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Operand, Operand, Operand>), this);
        }

        public override void Reset() {
            this.cacheBaseAddress = ulong.MaxValue;
            this.supressRIPIncrement = false;
            this.interruptsEnabled = true;
            this.inIsr = false;
            this.running = false;
            this.systemTickInterval = 50;
            this.instructionCacheEnabled = true;
            this.protectionMode = 0;

            this.registers = new ulong[0xFF];

            this.WriteRegister(Register.RF, ulong.MaxValue);
            this.WriteRegister(Register.RIP, (ulong)DeviceType.BootManager << 52);

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
            this.SetNextInstruction();

            if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
                this.LoadParameters(this.operandA, this.operandB, this.operandC);

                this.instructionHandlers[this.CurrentInstruction.Code](this.operandA, this.operandB, this.operandC);

                this.SaveParameters(this.operandA, this.operandB, this.operandC);

                if (!this.supressRIPIncrement) {
                    this.WriteRegister(Register.RIP, this.ReadRegister(Register.RIP) + this.CurrentInstruction.Length);
                }
                else {
                    this.supressRIPIncrement = false;
                }
            }
            else {
                this.InterruptController.Enqueue(Interrupt.InvalidInstruction, this.CurrentInstruction.Code, 0);
            }

            if (this.interruptsEnabled && !this.inIsr && this.InterruptController.PendingCount != 0)
                this.EnterInterrupt(this.InterruptController.Dequeue());
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

        private void EnterInterrupt(InterruptController.Entry interrupt) {
            var isr = this.InterruptController.GetVector(interrupt);

            if (isr == 0)
                return;

            this.WriteRegister(Register.RSIP, this.ReadRegister(Register.RIP));
            this.WriteRegister(Register.RIP, isr);
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
            else if (parameter.Type == ParameterType.Literal) {
                value = parameter.Literal;
            }
            else if (parameter.Type == ParameterType.Calculated) {
                value = this.GetCalculatedLiteral(parameter);
            }

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
                else if (parameter.Type == ParameterType.Literal) {
                    address = parameter.Literal;
                }
                else if (parameter.Type == ParameterType.Calculated) {
                    address = this.GetCalculatedLiteral(parameter);
                }

                this.BusController.WriteWord(address, value);
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

        public void Push(ulong value) {
            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) - 8);

            this.BusController.WriteWord(this.ReadRegister(Register.RSP), value);
        }

        public ulong Pop() {
            var value = this.BusController.ReadWord(this.ReadRegister(Register.RSP));

            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) + 8);

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
            else if (address == 0) {
                this.systemTickInterval = (byte)data;
            }
            else if (address == 0) {
                this.instructionCacheEnabled = data != 0;
            }
        }
    }
}