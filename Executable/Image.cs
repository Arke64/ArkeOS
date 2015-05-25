using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.Executable {
	public class Image {
		public Header Header { get; set; }
		public List<Section> Sections { get; set; }

		public Image() {
			this.Header = new Header();
			this.Sections = new List<Section>();
		}

		public Image(byte[] data) : this() {
			using (var reader = new BinaryReader(new MemoryStream(data))) {
				this.Header.Read(reader);

				reader.BaseStream.Seek(Header.Size, SeekOrigin.Begin);

				for (var i = 0; i < this.Header.SectionCount; i++)
					this.Sections.Add(new Section(reader));
			}
		}

		public byte[] ToArray() {
			using (var stream = new MemoryStream()) {
				using (var writer = new BinaryWriter(stream)) {
					this.Header.Write(writer);

					writer.BaseStream.Seek(Header.Size, SeekOrigin.Begin);

					this.Sections.ForEach(s => s.Serialize(writer, this));
				}

				return stream.ToArray();
			}
		}

		public Instruction FindByLabel(string label) {
			return this.Sections.Select(s => s.FindByLabel(label)).SingleOrDefault(s => s != null);
		}
	}
}