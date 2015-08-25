using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public partial class Processor {
        private static int MaxCachedInstuctions => 1024;

        private ProcessorController processorController;
        private SystemBusController systemBusController;
        private InterruptController interruptController;
        private Action<Operand, Operand, Operand>[] instructionHandlers;
        private Timer systemTimer;
        private Instruction[] cachedInstructions;
        private ulong[] registers;
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

        public event EventHandler ExecutionPaused;

        public Processor(SystemBusController systemBusController) {
            this.processorController = new ProcessorController();
            this.interruptController = new InterruptController();

            this.systemBusController = systemBusController;
            this.systemBusController.InterruptController = interruptController;
            this.systemBusController.AddDevice(SystemBusController.ProcessorControllerDeviceId, this.processorController);
            this.systemBusController.AddDevice(SystemBusController.InterruptControllerDeviceId, this.interruptController);

            this.operandA = new Operand();
            this.operandB = new Operand();
            this.operandC = new Operand();

            this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            this.instructionHandlers = new Action<Operand, Operand, Operand>[InstructionDefinition.All.Max(i => i.Code) + 1];

            foreach (var i in InstructionDefinition.All)
                this.instructionHandlers[i.Code] = (Action<Operand, Operand, Operand>)this.GetType().GetMethod("Execute" + i.Mnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Operand, Operand, Operand>), this);
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
            this.systemTimer.Change(this.processorController.SystemTickInterval, this.processorController.SystemTickInterval);

            new Task(() => {
                while (!this.broken)
                    this.Tick();
            }, TaskCreationOptions.LongRunning).Start();
        }

        public void Step() {
            this.Tick();
        }

        private void Reset() {
            this.registers = new ulong[0xFF];

            this.WriteRegister(Register.RF, ulong.MaxValue);
            this.WriteRegister(Register.RIP, 3UL << 52);

            this.processorController.Reset();
            this.systemBusController.EnumerateBus();

            this.cacheBaseAddress = ulong.MaxValue;
            this.supressRIPIncrement = false;
            this.interruptsEnabled = true;
            this.inProtectedIsr = false;
            this.inIsr = false;
        }

        private void OnSystemTimerTick(object state) {
            if (this.processorController.SystemTickInterval != 0)
                this.interruptController.Enqueue(Interrupt.SystemTimer, 0, 0);
        }

        private void SetNextInstruction() {
            var address = this.ReadRegister(Register.RIP);

            if (!this.processorController.InstructionCachingEnabled) {
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
                    this.WriteRegister(Register.RIP, this.ReadRegister(Register.RIP) + this.CurrentInstruction.Length);
                }
                else {
                    this.supressRIPIncrement = false;
                }
            }
            else {
                this.interruptController.Enqueue(Interrupt.InvalidInstruction, this.CurrentInstruction.Code, 0);
            }

            if (this.interruptsEnabled && !this.inIsr && this.interruptController.PendingCount != 0)
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

        private void EnterInterrupt(InterruptController.Entry interrupt) {
            var isr = this.interruptController.Vectors[(int)interrupt.Id];

            if (isr == 0)
                return;

            this.WriteRegister(Register.RSIP, this.ReadRegister(Register.RIP));
            this.WriteRegister(Register.RIP, isr);
            this.WriteRegister(Register.RINT1, interrupt.Data1);
            this.WriteRegister(Register.RINT2, interrupt.Data2);

            this.inProtectedIsr = (byte)interrupt.Id < 0x10;
            this.inIsr = true;
        }

        private ulong GetValue(Parameter parameter) {
            var value = 0UL;

            if (parameter.Type == ParameterType.Register) {
                if (this.IsRegisterReadAllowed(parameter.Register))
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
                value = this.systemBusController.ReadWord(value);

            return value;
        }

        private void SetValue(Parameter parameter, ulong value) {
            if (!parameter.IsIndirect) {
                if (parameter.Type == ParameterType.Register) {
                    if (this.IsRegisterWriteAllowed(parameter.Register)) {
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
                if (parameter.Type == ParameterType.Register) {
                    if (this.IsRegisterReadAllowed(parameter.Register))
                        this.systemBusController.WriteWord(this.ReadRegister(parameter.Register), value);
                }
                else if (parameter.Type == ParameterType.Stack) {
                    this.systemBusController.WriteWord(this.Pop(), value);
                }
                else if (parameter.Type == ParameterType.Literal) {
                    this.systemBusController.WriteWord(parameter.Literal, value);
                }
                else if (parameter.Type == ParameterType.Calculated) {
                    this.systemBusController.WriteWord(this.GetCalculatedLiteral(parameter), value);
                }
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
            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) - 8);

            this.systemBusController.WriteWord(this.ReadRegister(Register.RSP), value);
        }

        private ulong Pop() {
            var value = this.systemBusController.ReadWord(this.ReadRegister(Register.RSP));

            this.WriteRegister(Register.RSP, this.ReadRegister(Register.RSP) + 8);

            return value;
        }

        public ulong ReadRegister(Register register) => this.registers[(int)register];
        public void WriteRegister(Register register, ulong value) => this.registers[(int)register] = value;

        private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.processorController.ProtectionMode == 0 || (register != Register.RSIP && register != Register.RINT1 && register != Register.RINT2);
        private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.processorController.ProtectionMode == 0 || (register != Register.RSIP && register != Register.RINT1 && register != Register.RINT2 && register != Register.RO && register != Register.RF);
    }
}