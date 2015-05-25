using System;
using ArkeOS.ISA;

namespace ArkeOS.Interpreter {
	public partial class Interpreter {
		#region Basic

		private void Hlt(Instruction instruction) {
			this.running = false;
		}

		private void Nop(Instruction instruction) {

		}

		private void Int(Instruction instruction) {
			this.EnterInterrupt((Interrupt)this.GetValue(instruction.A));

			this.supressRIPIncrement = true;
		}

		private void Eint(Instruction instruction) {
			this.registers[Register.RIP] = this.registers[Register.RSIP];

			this.inProtectedIsr = false;
			this.supressRIPIncrement = true;
		}

		private void Mov(Instruction instruction) {
			this.Access(instruction.A, instruction.B, a => a);
		}

		private void Xchg(Instruction instruction) {
			this.SetValue(instruction.A, this.GetValue(instruction.A));
			this.SetValue(instruction.B, this.GetValue(instruction.B));
		}

		private void In(Instruction instruction) {

		}

		private void Out(Instruction instruction) {

		}

		private void Push(Instruction instruction) {
			this.registers[Register.RSP] -= instruction.SizeInBytes;

			switch (instruction.Size) {
				case InstructionSize.OneByte: this.Access(instruction.A, a => this.memory.WriteU8(this.registers[Register.RSP], (byte)a)); break;
				case InstructionSize.TwoByte: this.Access(instruction.A, a => this.memory.WriteU16(this.registers[Register.RSP], (ushort)a)); break;
				case InstructionSize.FourByte: this.Access(instruction.A, a => this.memory.WriteU32(this.registers[Register.RSP], (uint)a)); break;
				case InstructionSize.EightByte: this.Access(instruction.A, a => this.memory.WriteU64(this.registers[Register.RSP], a)); break;
			}
		}

		private void Pop(Instruction instruction) {
			this.registers[Register.RSP] += instruction.SizeInBytes;

			switch (instruction.Size) {
				case InstructionSize.OneByte: this.Access(instruction.A, () => this.memory.ReadU8(this.registers[Register.RSP])); break;
				case InstructionSize.TwoByte: this.Access(instruction.A, () => this.memory.ReadU16(this.registers[Register.RSP])); break;
				case InstructionSize.FourByte: this.Access(instruction.A, () => this.memory.ReadU32(this.registers[Register.RSP])); break;
				case InstructionSize.EightByte: this.Access(instruction.A, () => this.memory.ReadU64(this.registers[Register.RSP])); break;
			}
		}

		private void Jz(Instruction instruction) {
			if (this.GetValue(instruction.A) == 0) {
				this.registers[Register.RIP] = this.GetValue(instruction.B);

				this.supressRIPIncrement = true;
			}
		}

		private void Jnz(Instruction instruction) {
			if (this.GetValue(instruction.A) != 0) {
				this.registers[Register.RIP] = this.GetValue(instruction.B);

				this.supressRIPIncrement = true;
			}
		}

		private void Jmp(Instruction instruction) {
			this.registers[Register.RIP] = this.GetValue(instruction.A);

			this.supressRIPIncrement = true;
		}

		#endregion

		#region Math

		private void Add(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);
			var max = Instruction.SizeToMask(instruction.Size);

            if (max - a < b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.C, max & ((max & a) + (max & b)));
			}
		}

		private void Adc(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);
			var carry = this.registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Instruction.SizeToMask(instruction.Size);

			if (a < max) {
				a += carry;
			}
			else if (b < max) {
				b += carry;
			}
			else if (carry == 1) {
				this.registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.C, max);

				return;
			}

			if (max - a < b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.C, max & ((max & a) + (max & b)));
			}
		}

		private void Adf(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(aa + bb), 0);

			this.SetValue(instruction.C, cc);
		}

		private void Sub(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a > b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.C, max & ((max & b) - (max & a)));
			}
		}

		private void Sbb(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);
			var carry = this.registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Instruction.SizeToMask(instruction.Size);

			if (a > 0) {
				a -= carry;
			}
			else if (b > 0) {
				b -= carry;
			}
			else if (carry == 1) {
				this.registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.C, max);

				return;
			}

			if (a > b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.C, max & ((max & b) - (max & a)));
			}
		}

		private void Sbf(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb - aa), 0);

			this.SetValue(instruction.C, cc);
		}

		private void Div(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			if (a != 0) {
				this.SetValue(instruction.C, b / a);
			}
			else {
				this.pendingInterrupts.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void Dvf(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
				var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb / aa), 0);

				this.SetValue(instruction.C, cc);
			}
			else {
				this.pendingInterrupts.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void Mul(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a == 0) {
				var t = a;
				a = b;
				b = t;
			}

			if (a == 0) {
				this.SetValue(instruction.C, 0);

				return;
			}

			if (max / a > b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.C, max & ((max & a) * (max & b)));
			}
		}

		private void Mlf(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb * aa), 0);

			this.SetValue(instruction.C, cc);
		}

		private void Inc(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var max = Instruction.SizeToMask(instruction.Size);

			if (max == a)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.B, a + 1);
			}
		}

		private void Dec(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var max = Instruction.SizeToMask(instruction.Size);

			if (a == 0)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.B, a - 1);
			}
		}

		private void Neg(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var mask = (ulong)(1 << (Instruction.SizeToBits(this.currentSize) - 1));

			if ((a & mask) == 0) {
				a |= mask;
			}
			else {
				a &= ~mask;
			}

			this.SetValue(instruction.B, a);
		}

		private void Mod(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			if (a == 0)
				throw new DivideByZeroException();

			this.SetValue(instruction.C, b % a);
		}

		private void Mdf(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			if (a == 0)
				throw new DivideByZeroException();

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb % aa), 0);

			this.SetValue(instruction.C, cc);
		}

		#endregion

		#region Logic

		private void Sr(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => b >> (byte)a);
		}

		private void Sl(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => b << (byte)a);
		}

		private void Rr(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => (b >> (byte)a) | (b << (Instruction.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void Rl(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => (b << (byte)a) | (b >> (Instruction.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void Nand(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => ~(a & b));
		}

		private void And(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a & b);
		}

		private void Nor(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => ~(a | b));
		}

		private void Or(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a | b);
		}

		private void Nxor(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => ~(a ^ b));
		}

		private void Xor(Instruction instruction) {
			this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a ^ b);
		}

		private void Not(Instruction instruction) {
			this.Access(instruction.A, instruction.B, a => ~a);
		}

		private void Gt(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b > a ? ulong.MaxValue : 0);
		}

		private void Gte(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b >= a ? ulong.MaxValue : 0);
		}

		private void Lt(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b < a ? ulong.MaxValue : 0);
		}

		private void Lte(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b <= a ? ulong.MaxValue : 0);
		}

		private void Eq(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b == a ? ulong.MaxValue : 0);
		}

		private void Neq(Instruction instruction) {
			this.Access(instruction.A, instruction.B, (a, b) => this.registers[Register.RZ] = b != a ? ulong.MaxValue : 0);
		}

		#endregion
	}
}