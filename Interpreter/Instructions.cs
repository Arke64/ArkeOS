using System;
using ArkeOS.Executable;

namespace ArkeOS.Interpreter {
	public partial class Interpreter {
		#region Basic

		private void Hlt(Instruction instruction) {
			this.running = false;
		}

		private void Nop(Instruction instruction) {

		}

		private void Int(Instruction instruction) {

		}

		private void Mov(Instruction instruction) {
			this.Access(instruction.A, instruction.B, a => a);
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

		}

		private void Dvf(Instruction instruction) {

		}

		private void Mul(Instruction instruction) {

		}

		private void Mlf(Instruction instruction) {

		}

		private void Inc(Instruction instruction) {

		}

		private void Dec(Instruction instruction) {

		}

		private void Neg(Instruction instruction) {

		}

		private void Mod(Instruction instruction) {

		}

		private void Mdf(Instruction instruction) {

		}

		#endregion

		#region Logic

		private void Rr(Instruction instruction) {

		}

		private void Rl(Instruction instruction) {

		}

		private void Rrc(Instruction instruction) {

		}

		private void Rlc(Instruction instruction) {

		}

		private void Xchg(Instruction instruction) {

		}

		private void Nand(Instruction instruction) {

		}

		private void And(Instruction instruction) {

		}

		private void Nor(Instruction instruction) {

		}

		private void Or(Instruction instruction) {

		}

		private void Nxor(Instruction instruction) {

		}

		private void Xor(Instruction instruction) {

		}

		private void Not(Instruction instruction) {

		}

		private void Gt(Instruction instruction) {

		}

		private void Gte(Instruction instruction) {

		}

		private void Lt(Instruction instruction) {

		}

		private void Lte(Instruction instruction) {

		}

		private void Eq(Instruction instruction) {

		}

		private void Neq(Instruction instruction) {

		}

		#endregion
	}
}
