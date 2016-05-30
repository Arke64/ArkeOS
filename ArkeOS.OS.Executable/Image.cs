using System.Collections.Generic;
using System.IO;

namespace ArkeOS.OS.Executable {
	public class Image {
		public Header Header { get; private set; }
		public List<Section> Sections { get; private set; }

		public Image() {
			this.Header = new Header();
			this.Sections = new List<Section>();
		}

		public Image(Stream data) {
			this.Sections = new List<Section>();

			using (var reader = new BinaryReader(data)) {
				this.Header = new Header(reader);

				reader.BaseStream.Seek(Header.Size, SeekOrigin.Begin);

				for (var i = 0; i < this.Header.SectionCount; i++)
					this.Sections.Add(new Section(reader));
			}
		}

		public byte[] ToArray() {
			this.Header.SectionCount = (ushort)this.Sections.Count;

			using (var stream = new MemoryStream()) {
				using (var writer = new BinaryWriter(stream)) {
					this.Header.Serialize(writer);

					writer.BaseStream.Seek(Header.Size, SeekOrigin.Begin);

					foreach (var s in this.Sections)
						s.Serialize(writer);
				}

				return stream.ToArray();
			}
		}
	}
}