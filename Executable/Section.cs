using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.Executable {
	public class Section {
		private List<Instruction> instructions;

		public ulong Address { get; }
		public ulong Size { get; private set; }
		public byte[] Data { get; private set; }

		public Section(ulong address) {
			this.instructions = new List<Instruction>();

			this.Address = address;
		}

		public Section(BinaryReader reader) {
			this.Address = reader.ReadUInt64();
			this.Size = reader.ReadUInt64();
			this.Data = reader.ReadBytes((int)this.Size);
		}

		public void AddInstruction(Instruction instruction) {
			this.instructions.Add(instruction);
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write(this.Address);
			writer.Write((ulong)this.instructions.Sum(i => i.Length));

			var start = writer.BaseStream.Position;

			foreach (var i in this.instructions) {
				i.Address = (ulong)(writer.BaseStream.Position - start) + this.Address;
				i.Serialize(writer);
			}
		}
	}
}