using System;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public partial class Processor {
        #region Basic

        private void ExecuteHLT(Operand a, Operand b, Operand c) {
            this.InterruptController.Wait(50);
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

        private void ExecuteMOV(Operand a, Operand b, Operand c) {
            b.Value = a.Value;
        }

        private void ExecuteMVZ(Operand a, Operand b, Operand c) {
            if (a.Value == 0)
                c.Value = b.Value;
        }

        private void ExecuteMVNZ(Operand a, Operand b, Operand c) {
            if (a.Value != 0)
                c.Value = b.Value;
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
            a.Value = (ulong)DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
        }

        private void ExecuteBRK(Operand a, Operand b, Operand c) {
            this.Break();

            this.ExecutionBroken?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}