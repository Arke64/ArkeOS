using System.IO;

namespace ArkeOS.Executable {
	public class Header {
		public static int Size => 32;
		public static ushort MagicNumber => 0x4447;

		public ushort Magic { get; set; }
		public ushort SectionCount { get; set; }
		public ulong EntryPoint { get; set; }
		public ulong StackLocation { get; set; }
		public ulong StackSize { get; set; }

		public void Write(BinaryWriter writer) {
			writer.Write(this.Magic);
			writer.Write(this.SectionCount);
			writer.Write(this.EntryPoint);
			writer.Write(this.StackLocation);
			writer.Write(this.StackSize);
		}

		public void Read(BinaryReader reader) {
			this.Magic = reader.ReadUInt16();
			this.SectionCount = reader.ReadUInt16();
			this.EntryPoint = reader.ReadUInt64();
			this.StackLocation = reader.ReadUInt64();
			this.StackSize = reader.ReadUInt64();
		}
	}
}