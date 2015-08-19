using System;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		#region Basic

		private void ExecuteHLT(Operand a, Operand b, Operand c) {
			this.interruptController.Wait(50);
			this.supressRIPIncrement = true;
		}

		private void ExecuteNOP(Operand a, Operand b, Operand c) {

		}

		private void ExecuteINT(Operand a, Operand b, Operand c) {
			this.interruptController.Enqueue((Interrupt)a.Value);
		}

		private void ExecuteEINT(Operand a, Operand b, Operand c) {
			this.Registers[Register.RIP] = this.Registers[Register.RSIP];

			this.inIsr = false;
			this.inProtectedIsr = false;
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

		private void ExecuteIN(Operand a, Operand b, Operand c) {

		}

		private void ExecuteOUT(Operand a, Operand b, Operand c) {

		}

		#endregion

		#region Math

		private void ExecuteADD(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = a.Value + b.Value;
			}
		}

		private void ExecuteADDF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a.Value), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b.Value), 0);

			c.Value = BitConverter.ToUInt64(BitConverter.GetBytes(aa + bb), 0);
		}

		private void ExecuteSUB(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = b.Value - a.Value;
			}
		}

		private void ExecuteSUBF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a.Value), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b.Value), 0);

			c.Value = BitConverter.ToUInt64(BitConverter.GetBytes(bb - aa), 0);
		}

		private void ExecuteDIV(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				c.Value = b.Value / a.Value;
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteDIVF(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a.Value), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b.Value), 0);

				c.Value = BitConverter.ToUInt64(BitConverter.GetBytes(bb / aa), 0);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteMUL(Operand a, Operand b, Operand c) {
			unchecked {
				c.Value = a.Value * b.Value;
			}
		}

		private void ExecuteMULF(Operand a, Operand b, Operand c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a.Value), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b.Value), 0);

			c.Value = BitConverter.ToUInt64(BitConverter.GetBytes(bb * aa), 0);
		}

		private void ExecuteMOD(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				c.Value = b.Value % a.Value;
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteMODF(Operand a, Operand b, Operand c) {
			if (a.Value != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a.Value), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b.Value), 0);

				c.Value = BitConverter.ToUInt64(BitConverter.GetBytes(bb % aa), 0);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
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

			this.ExecutionPaused?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}