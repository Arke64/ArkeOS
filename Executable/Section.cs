using System.IO;

namespace ArkeOS.Executable {
	public class Section {
		public ulong Address { get; }
		public ulong Size { get; }
		public byte[] Data { get; }

		public Section(ulong address, byte[] data) {
			this.Address = address;
			this.Data = data;
			this.Size = (ulong)data.Length;
		}

		public Section(BinaryReader reader) {
			this.Address = reader.ReadUInt64();
			this.Size = reader.ReadUInt64();
			this.Data = reader.ReadBytes((int)this.Size);
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write(this.Address);
			writer.Write(this.Size);
			writer.Write(this.Data);
		}
	}
}