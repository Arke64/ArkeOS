using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Executable {
	public class Section {
		private List<Instruction> instructions;
		private ulong currentAddress;

		public ulong Address { get; }
		public ulong Size { get; private set; }
		public byte[] Data { get; private set; }

		public Dictionary<string, Instruction> Labels => this.instructions.Where(i => !string.IsNullOrWhiteSpace(i.Label)).ToDictionary(i => i.Label, i => i);

		public Section(ulong address) {
			this.instructions = new List<Instruction>();
			this.currentAddress = 0;

			this.Address = address;
		}

		public Section(BinaryReader reader) {
			this.Address = reader.ReadUInt64();
			this.Size = reader.ReadUInt64();
			this.Data = reader.ReadBytes((int)this.Size);
		}

		public void AddInstruction(Instruction instruction) {
			instruction.Address = this.currentAddress;

			this.currentAddress += instruction.Length;
			this.instructions.Add(instruction);
		}

		public void Serialize(BinaryWriter writer, Image parent) {
			writer.Write(this.Address);
			writer.Write((ulong)this.instructions.Sum(i => i.Length));

			this.instructions.ForEach(i => i.Serialize(writer, parent.Labels));
		}
	}
}