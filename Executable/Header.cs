using System.IO;

namespace ArkeOS.Executable {
	public class Header {
		public static int Size => 32;
		public static ushort MagicNumber => 0x4744;

		public ushort Magic { get; set; }
		public ushort SectionCount { get; set; }
		public ulong EntryPointAddress { get; set; }
		public ulong StackAddress { get; set; }

		public void Write(BinaryWriter writer) {
			writer.Write(this.Magic);
			writer.Write(this.SectionCount);
			writer.Write(this.EntryPointAddress);
			writer.Write(this.StackAddress);
		}

		public void Read(BinaryReader reader) {
			this.Magic = reader.ReadUInt16();
			this.SectionCount = reader.ReadUInt16();
			this.EntryPointAddress = reader.ReadUInt64();
			this.StackAddress = reader.ReadUInt64();
		}
	}
}