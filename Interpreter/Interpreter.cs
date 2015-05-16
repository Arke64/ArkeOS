using System.Linq;
using System.IO;
using ArkeOS.Executable;

namespace ArkeOS.Interpreter {
	public class Interpreter {
		private Image image;
		private MemoryManager memory;
		private ulong instructionPointer;
		private ulong stackPointer;

		public Interpreter() {
			this.image = new Image();
			this.memory = new MemoryManager();
			this.instructionPointer = 0;
			this.stackPointer = 0;
		}

		public void Parse(byte[] data) {
			this.image.FromArray(data);

			if (this.image.Header.Magic != Header.MagicNumber) throw new InvalidProgramFormatException();
			if (!this.image.Sections.Any()) throw new InvalidProgramFormatException();

			this.image.Sections.ForEach(s => this.memory.CopyFrom(s.Data, s.Address, s.Size));

			this.instructionPointer = this.image.Header.EntryPointAddress;
			this.stackPointer = this.image.Header.StackAddress;

			this.image = null;
		}

		public void Run() {
			while (true) {
				this.memory.Reader.BaseStream.Seek((long)this.instructionPointer, SeekOrigin.Begin);

				var instruction = new Instruction(this.memory.Reader);

				if (instruction.Code == Instruction.Hlt.Code) {
					return;
				}
				else if (instruction.Code == Instruction.Nop.Code) {
					
				}
				else if (instruction.Code == Instruction.Add.Code) {

				}
				else if (instruction.Code == Instruction.Jiz.Code) {

				}
				else {
					throw new InvalidInstructionException();
				}

				this.instructionPointer += instruction.Length;
			}
		}
	}
}