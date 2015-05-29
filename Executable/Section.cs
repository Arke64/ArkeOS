using System.IO;

namespace ArkeOS.Executable {
	public abstract class Section {
		protected Image Parent { get; private set; }
		protected ulong CurrentAddress { get; set; }

		public ulong Address { get; }
		public ulong Size { get; protected set; }
		public bool IsCode { get; private set; }
		public byte[] Data { get; private set; }

		public Section(Image parent, ulong address, bool isCode) {
			this.CurrentAddress = address;
			this.Parent = parent;

			this.IsCode = isCode;
			this.Address = address;
		}

		public Section(ulong address, ulong size, bool isCode, byte[] data) {
			this.Address = address;
			this.Size = size;
			this.IsCode = isCode;
			this.Data = data;
		}

		public static Section Parse(BinaryReader reader) {
			var address = reader.ReadUInt64();
			var size = reader.ReadUInt64();
			var isCode = reader.ReadBoolean();
			var data = reader.ReadBytes((int)size);

			return isCode ? new CodeSection(address, size, data) : (Section)(new DataSection(address, size, data));
		}

		public virtual void Serialize(BinaryWriter writer) {
			writer.Write(this.Address);
			writer.Write(this.Size);
			writer.Write(this.IsCode);
		}
	}
}