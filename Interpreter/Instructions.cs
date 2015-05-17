using ArkeOS.Executable;

namespace ArkeOS.Interpreter {
	public partial class Interpreter {
		private void AddInstructions() {
			this.instructionHandlers.Add(InstructionDefinition.Hlt, this.Hlt);
			this.instructionHandlers.Add(InstructionDefinition.Nop, this.Nop);
			this.instructionHandlers.Add(InstructionDefinition.Add, this.Add);
		}

		private void Hlt(Instruction instruction) {
			this.running = false;
		}

		private void Nop(Instruction instruction) {

		}

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
	}
}
