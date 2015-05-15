using System.IO;

namespace ArkeOS.Executable {
	public class Section {
		public ulong Address { get; set; }
		public ulong Size { get; set; }
		public byte[] Data { get; set; }

		public void Write(BinaryWriter writer) {
			writer.Write(this.Address);
			writer.Write(this.Size);
			writer.Write(this.Data, 0, (int)this.Size);
		}

		public void Read(BinaryReader reader) {
			this.Address = reader.ReadUInt64();
			this.Size = reader.ReadUInt64();
			this.Data = reader.ReadBytes((int)this.Size);
		}
	}
}