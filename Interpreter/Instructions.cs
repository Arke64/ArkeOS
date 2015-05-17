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

			var max = 0UL;
			switch (instruction.Size) {
				case InstructionSize.OneByte: max = byte.MaxValue; break;
				case InstructionSize.TwoByte: max = ushort.MaxValue; break;
				case InstructionSize.FourByte: max = uint.MaxValue; break;
				case InstructionSize.EightByte: max = ulong.MaxValue; break;
			}

			if (max - a < b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				switch (instruction.Size) {
					case InstructionSize.OneByte: this.SetValue(instruction.C, (byte)((byte)b + (byte)a)); break;
					case InstructionSize.TwoByte: this.SetValue(instruction.C, (ushort)((ushort)b + (ushort)a)); break;
					case InstructionSize.FourByte: this.SetValue(instruction.C, (uint)b + (uint)a); break;
					case InstructionSize.EightByte: this.SetValue(instruction.C, b + a); break;
				}
			}
		}

		private void Adc(Instruction instruction) {

		}

		private void Adf(Instruction instruction) {

		}

		private void Sub(Instruction instruction) {
			var a = this.GetValue(instruction.A);
			var b = this.GetValue(instruction.B);

			if (a > b)
				this.registers[Register.RC] = ulong.MaxValue;

			unchecked {
				switch (instruction.Size) {
					case InstructionSize.OneByte: this.SetValue(instruction.C, (byte)((byte)b - (byte)a)); break;
					case InstructionSize.TwoByte: this.SetValue(instruction.C, (ushort)((ushort)b - (ushort)a)); break;
					case InstructionSize.FourByte: this.SetValue(instruction.C, (uint)b - (uint)a); break;
					case InstructionSize.EightByte: this.SetValue(instruction.C, b - a); break;
				}
			}
		}

		private void Sbb(Instruction instruction) {

		}

		private void Sbf(Instruction instruction) {

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
