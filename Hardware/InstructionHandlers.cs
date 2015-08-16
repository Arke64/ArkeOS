using System;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		#region Basic

		private void ExecuteHLT(ref ulong a, ref ulong b, ref ulong c) {
			this.interruptController.Wait(50);
			this.supressRIPIncrement = true;
		}

		private void ExecuteNOP(ref ulong a, ref ulong b, ref ulong c) {

		}

		private void ExecuteINT(ref ulong a, ref ulong b, ref ulong c) {
			this.interruptController.Enqueue((Interrupt)a);
		}

		private void ExecuteEINT(ref ulong a, ref ulong b, ref ulong c) {
			this.Registers[Register.RIP] = this.Registers[Register.RSIP];

			this.inIsr = false;
			this.inProtectedIsr = false;
			this.supressRIPIncrement = true;
		}

		private void ExecuteINTE(ref ulong a, ref ulong b, ref ulong c) {
			this.interruptsEnabled = true;
		}

		private void ExecuteINTD(ref ulong a, ref ulong b, ref ulong c) {
			this.interruptsEnabled = false;
		}

		private void ExecuteMOV(ref ulong a, ref ulong b, ref ulong c) {
			b = a;
		}

		private void ExecuteXCHG(ref ulong a, ref ulong b, ref ulong c) {
			var t = a;

			a = b;
			b = t;
		}

		private void ExecuteIN(ref ulong a, ref ulong b, ref ulong c) {

		}

		private void ExecuteOUT(ref ulong a, ref ulong b, ref ulong c) {

		}

		private void ExecuteJZ(ref ulong a, ref ulong b, ref ulong c) {
			if (a == 0) {
				this.Registers[Register.RIP] = b;

				this.supressRIPIncrement = true;
			}
		}

		private void ExecuteJNZ(ref ulong a, ref ulong b, ref ulong c) {
			if (a != 0) {
				this.Registers[Register.RIP] = b;

				this.supressRIPIncrement = true;
			}
		}

		private void ExecuteJMP(ref ulong a, ref ulong b, ref ulong c) {
			this.Registers[Register.RIP] = a;

			this.supressRIPIncrement = true;
		}

		private void ExecutePUSH(ref ulong a, ref ulong b, ref ulong c) {
			this.Registers[Register.RSP] -= Helpers.SizeToBytes(this.CurrentInstruction.Size);

			switch (this.CurrentInstruction.Size) {
				case InstructionSize.OneByte: this.memoryController.WriteU8(this.Registers[Register.RSP], (byte)a); break;
				case InstructionSize.TwoByte: this.memoryController.WriteU16(this.Registers[Register.RSP], (ushort)a); break;
				case InstructionSize.FourByte: this.memoryController.WriteU32(this.Registers[Register.RSP], (uint)a); break;
				case InstructionSize.EightByte: this.memoryController.WriteU64(this.Registers[Register.RSP], a); break;
			}
		}

		private void ExecutePOP(ref ulong a, ref ulong b, ref ulong c) {
			var value = 0UL;

			switch (this.CurrentInstruction.Size) {
				case InstructionSize.OneByte: value = this.memoryController.ReadU8(this.Registers[Register.RSP]); break;
				case InstructionSize.TwoByte: value = this.memoryController.ReadU16(this.Registers[Register.RSP]); break;
				case InstructionSize.FourByte: value = this.memoryController.ReadU32(this.Registers[Register.RSP]); break;
				case InstructionSize.EightByte: value = this.memoryController.ReadU64(this.Registers[Register.RSP]); break;
			}

			this.Registers[Register.RSP] += Helpers.SizeToBytes(this.CurrentInstruction.Size);

			a = value;
		}

		#endregion

		#region Math

		private void ExecuteADD(ref ulong a, ref ulong b, ref ulong c) {
			var max = Helpers.SizeToMask(this.CurrentInstruction.Size);

			if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				c = max & ((max & a) + (max & b));
			}
		}

		private void ExecuteADDC(ref ulong a, ref ulong b, ref ulong c) {
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Helpers.SizeToMask(this.CurrentInstruction.Size);

			if (a < max) {
				a += carry;
			}
			else if (b < max) {
				b += carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				c = max;
			}

			if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				c = max & ((max & a) + (max & b));
			}
		}

		private void ExecuteADDF(ref ulong a, ref ulong b, ref ulong c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);

			c = BitConverter.ToUInt64(BitConverter.GetBytes(aa + bb), 0);
		}

		private void ExecuteSUB(ref ulong a, ref ulong b, ref ulong c) {
			var max = Helpers.SizeToMask(this.CurrentInstruction.Size);

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				c = max & ((max & b) - (max & a));
			}
		}

		private void ExecuteSUBC(ref ulong a, ref ulong b, ref ulong c) {
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Helpers.SizeToMask(this.CurrentInstruction.Size);

			if (a > 0) {
				a -= carry;
			}
			else if (b > 0) {
				b -= carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				return;
			}

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				c = max & ((max & b) - (max & a));
			}
		}

		private void ExecuteSUBF(ref ulong a, ref ulong b, ref ulong c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);

			c = BitConverter.ToUInt64(BitConverter.GetBytes(bb - aa), 0);
		}

		private void ExecuteDIV(ref ulong a, ref ulong b, ref ulong c) {
			if (a != 0) {
				c = b / a;
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteDIVF(ref ulong a, ref ulong b, ref ulong c) {
			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);

				c = BitConverter.ToUInt64(BitConverter.GetBytes(bb / aa), 0);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteMUL(ref ulong a, ref ulong b, ref ulong c) {
			var max = Helpers.SizeToMask(this.CurrentInstruction.Size);

			if (a == 0) {
				var t = a;

				a = b;
				b = t;
			}

			if (a == 0)
				return;

			if (max / a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				c = max & ((max & a) * (max & b));
			}
		}

		private void ExecuteMULF(ref ulong a, ref ulong b, ref ulong c) {
			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);

			c = BitConverter.ToUInt64(BitConverter.GetBytes(bb * aa), 0);
		}

		private void ExecuteMOD(ref ulong a, ref ulong b, ref ulong c) {
			if (a != 0) {
				c = b % a;
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteMODF(ref ulong a, ref ulong b, ref ulong c) {
			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);

				c = BitConverter.ToUInt64(BitConverter.GetBytes(bb % aa), 0);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		#endregion

		#region Logic

		private void ExecuteSR(ref ulong a, ref ulong b, ref ulong c) => c = b >> (byte)a;
		private void ExecuteSL(ref ulong a, ref ulong b, ref ulong c) => c = b << (byte)a;
		private void ExecuteRR(ref ulong a, ref ulong b, ref ulong c) => c = (b >> (byte)a) | (b << (Helpers.SizeToBits(this.CurrentInstruction.Size) - (byte)a));
		private void ExecuteRL(ref ulong a, ref ulong b, ref ulong c) => c = (b << (byte)a) | (b >> (Helpers.SizeToBits(this.CurrentInstruction.Size) - (byte)a));
		private void ExecuteNAND(ref ulong a, ref ulong b, ref ulong c) => c = ~(a & b);
		private void ExecuteAND(ref ulong a, ref ulong b, ref ulong c) => c = a & b;
		private void ExecuteNOR(ref ulong a, ref ulong b, ref ulong c) => c = ~(a | b);
		private void ExecuteOR(ref ulong a, ref ulong b, ref ulong c) => c = a | b;
		private void ExecuteNXOR(ref ulong a, ref ulong b, ref ulong c) => c = ~(a ^ b);
		private void ExecuteXOR(ref ulong a, ref ulong b, ref ulong c) => c = a ^ b;
		private void ExecuteNOT(ref ulong a, ref ulong b, ref ulong c) => b = ~a;
		private void ExecuteGT(ref ulong a, ref ulong b, ref ulong c) => c = b > a ? ulong.MaxValue : 0;
		private void ExecuteGTE(ref ulong a, ref ulong b, ref ulong c) => c = b >= a ? ulong.MaxValue : 0;
		private void ExecuteLT(ref ulong a, ref ulong b, ref ulong c) => c = b < a ? ulong.MaxValue : 0;
		private void ExecuteLTE(ref ulong a, ref ulong b, ref ulong c) => c = b <= a ? ulong.MaxValue : 0;
		private void ExecuteEQ(ref ulong a, ref ulong b, ref ulong c) => c = b == a ? ulong.MaxValue : 0;
		private void ExecuteNEQ(ref ulong a, ref ulong b, ref ulong c) => c = b != a ? ulong.MaxValue : 0;

		#endregion

		#region Debug

		private void ExecuteDBG(ref ulong a, ref ulong b, ref ulong c) {
			a = (ulong)DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
		}

		private void ExecutePAU(ref ulong a, ref ulong b, ref ulong c) {
			this.Break();

			this.ExecutionPaused?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}