using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Executable {
	public class CodeSection : Section {
		private List<Instruction> instructions;

		public CodeSection(Image parent, ulong address) : base(parent, address, true) {
			this.instructions = new List<Instruction>();
		}

		public CodeSection(ulong address, ulong size, byte[] data) : base(address, size, true, data) {

		}

		public void AddInstruction(Instruction instruction, string pendingLabel) {
			instruction.Address = this.CurrentAddress;
			instruction.Label = pendingLabel;

			if (pendingLabel != string.Empty)
				this.Parent.Labels[pendingLabel] = instruction.Address;

			this.CurrentAddress += instruction.Length;

			this.instructions.Add(instruction);
		}

		public override void Serialize(BinaryWriter writer) {
			this.Size = (ulong)this.instructions.Sum(i => i.Length);

			base.Serialize(writer);

			//this.instructions.ForEach(i => i.Serialize(writer, this.Parent.Labels));
		}
	}
}