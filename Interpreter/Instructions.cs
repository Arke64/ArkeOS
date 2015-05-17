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

		}

		private void Pop(Instruction instruction) {

		}

		private void Jz(Instruction instruction) {

		}

		private void Jnz(Instruction instruction) {

		}

		#endregion

		#region Math

		private void Add(Instruction instruction) {
			var aa = this.GetValue(instruction.A);
			var bb = this.GetValue(instruction.B);

			if (this.currentSize == InstructionSize.EightByte && ulong.MaxValue - aa < bb) {
				unchecked {
					this.SetValue(instruction.C, aa + bb);
				}

				this.registers[Register.RC] = ulong.MaxValue;
			}
			else {
				this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a + b);
			}
		}

		private void Adc(Instruction instruction) {

		}

		private void Adf(Instruction instruction) {

		}

		private void Sub(Instruction instruction) {

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
