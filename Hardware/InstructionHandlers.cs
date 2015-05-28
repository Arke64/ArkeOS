using System;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public partial class Processor {
		#region Basic

		private void Hlt(Instruction instruction) {
			this.interruptController.Wait(50);
			this.supressRIPIncrement = true;
		}

		private void Nop(Instruction instruction) {

		}

		private void Int(Instruction instruction) {
			this.EnterInterrupt((Interrupt)this.GetValue(instruction.Parameter1));

			this.supressRIPIncrement = true;
		}

		private void Eint(Instruction instruction) {
			this.Registers[Register.RIP] = this.Registers[Register.RSIP];

			this.inIsr = false;
			this.inProtectedIsr = false;
			this.supressRIPIncrement = true;
		}

		private void Mov(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, a => a);
		}

		private void Xchg(Instruction instruction) {
			this.SetValue(instruction.Parameter1, this.GetValue(instruction.Parameter1));
			this.SetValue(instruction.Parameter2, this.GetValue(instruction.Parameter2));
		}

		private void In(Instruction instruction) {

		}

		private void Out(Instruction instruction) {

		}

		private void Push(Instruction instruction) {
			this.Access(instruction.Parameter1, a => this.Push(instruction.Size, a));
		}

		private void Pop(Instruction instruction) {
			this.Access(instruction.Parameter1, () => this.Pop(instruction.Size));
		}

		private void Jz(Instruction instruction) {
			if (this.GetValue(instruction.Parameter1) == 0) {
				this.Registers[Register.RIP] = this.GetValue(instruction.Parameter2);

				this.supressRIPIncrement = true;
			}
		}

		private void Jnz(Instruction instruction) {
			if (this.GetValue(instruction.Parameter1) != 0) {
				this.Registers[Register.RIP] = this.GetValue(instruction.Parameter2);

				this.supressRIPIncrement = true;
			}
		}

		private void Jmp(Instruction instruction) {
			this.Registers[Register.RIP] = this.GetValue(instruction.Parameter1);

			this.supressRIPIncrement = true;
		}

		private void Call(Instruction instruction) {
			this.Push(instruction.Size, this.Registers[Register.RIP] + instruction.Length);

			this.Registers[Register.RIP] = this.GetValue(instruction.Parameter1);

			this.supressRIPIncrement = true;
		}

		private void Ret(Instruction instruction) {
			this.Registers[Register.RIP] = this.Pop(instruction.Size);

			this.supressRIPIncrement = true;
		}

		#endregion

		#region Math

		private void Add(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Instruction.SizeToMask(instruction.Size);

            if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) + (max & b)));
			}
		}

		private void Adc(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Instruction.SizeToMask(instruction.Size);

			if (a < max) {
				a += carry;
			}
			else if (b < max) {
				b += carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.Parameter3, max);

				return;
			}

			if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) + (max & b)));
			}
		}

		private void Adf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(aa + bb), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void Sub(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & b) - (max & a)));
			}
		}

		private void Sbb(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Instruction.SizeToMask(instruction.Size);

			if (a > 0) {
				a -= carry;
			}
			else if (b > 0) {
				b -= carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.Parameter3, max);

				return;
			}

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & b) - (max & a)));
			}
		}

		private void Sbf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb - aa), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void Div(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				this.SetValue(instruction.Parameter3, b / a);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void Dvf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
				var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb / aa), 0);

				this.SetValue(instruction.Parameter3, cc);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void Mul(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a == 0) {
				var t = a;
				a = b;
				b = t;
			}

			if (a == 0) {
				this.SetValue(instruction.Parameter3, 0);

				return;
			}

			if (max / a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) * (max & b)));
			}
		}

		private void Mlf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb * aa), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void Inc(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var max = Instruction.SizeToMask(instruction.Size);

			if (max == a)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter2, a + 1);
			}
		}

		private void Dec(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a == 0)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter2, a - 1);
			}
		}

		private void Neg(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var mask = (ulong)(1 << (this.CurrentInstruction.SizeInBits - 1));

			if ((a & mask) == 0) {
				a |= mask;
			}
			else {
				a &= ~mask;
			}

			this.SetValue(instruction.Parameter2, a);
		}

		private void Mod(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a == 0)
				throw new DivideByZeroException();

			this.SetValue(instruction.Parameter3, b % a);
		}

		private void Mdf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a == 0)
				throw new DivideByZeroException();

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb % aa), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		#endregion

		#region Logic

		private void Sr(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => b >> (byte)a);
		}

		private void Sl(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => b << (byte)a);
		}

		private void Rr(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => (b >> (byte)a) | (b << (Instruction.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void Rl(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => (b << (byte)a) | (b >> (Instruction.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void Nand(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => ~(a & b));
		}

		private void And(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => a & b);
		}

		private void Nor(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => ~(a | b));
		}

		private void Or(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => a | b);
		}

		private void Nxor(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => ~(a ^ b));
		}

		private void Xor(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, instruction.Parameter3, (a, b) => a ^ b);
		}

		private void Not(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, a => ~a);
		}

		private void Gt(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b > a ? ulong.MaxValue : 0);
		}

		private void Gte(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b >= a ? ulong.MaxValue : 0);
		}

		private void Lt(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b < a ? ulong.MaxValue : 0);
		}

		private void Lte(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b <= a ? ulong.MaxValue : 0);
		}

		private void Eq(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b == a ? ulong.MaxValue : 0);
		}

		private void Neq(Instruction instruction) {
			this.Access(instruction.Parameter1, instruction.Parameter2, (a, b) => this.Registers[Register.RZ] = b != a ? ulong.MaxValue : 0);
		}

		#endregion

		#region Debug

		private void Dbg(Instruction instruction) {
			this.Access(instruction.Parameter1, () => (ulong)DateTime.UtcNow.TimeOfDay.TotalMilliseconds);
		}

		#endregion
	}
}