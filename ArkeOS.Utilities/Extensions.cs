using System.IO;

namespace ArkeOS.Utilities.Extensions {
	public static class BinaryWriterExtensions {
		public static void WriteAt(this BinaryWriter self, ulong data, long index) {
			var current = self.BaseStream.Position;

			self.BaseStream.Seek(index, SeekOrigin.Begin);
			self.Write(data);
			self.BaseStream.Seek(current, SeekOrigin.Begin);
		}
	}
}
