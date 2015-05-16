using System.Linq;
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
				switch (this.memory.ReadU8(this.instructionPointer++)) {
					case Instructions.Halt:
						return;

					default:
						throw new InvalidInstructionException();
				}
			}
		}
	}
}