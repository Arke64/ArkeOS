using System.IO;

namespace ArkeOS.Executable {
	public class Header {
		public static int Size => 32;
		public static ushort MagicNumber => 0x4744;

		public ushort Magic { get; }
		public ushort SectionCount { get; set; }
		public ulong EntryPointAddress { get; set; }
		public ulong StackAddress { get; set; }

		public Header() {
			this.Magic = Header.MagicNumber;
			this.SectionCount = 0;
			this.EntryPointAddress = 0;
			this.StackAddress = 0;
		}

		public Header(BinaryReader reader) {
			this.Magic = reader.ReadUInt16();
			this.SectionCount = reader.ReadUInt16();
			this.EntryPointAddress = reader.ReadUInt64();
			this.StackAddress = reader.ReadUInt64();
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write(this.Magic);
			writer.Write(this.SectionCount);
			writer.Write(this.EntryPointAddress);
			writer.Write(this.StackAddress);
		}
	}
}