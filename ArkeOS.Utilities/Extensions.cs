using System;
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

	public static class UlongExtensions {
		public static string ToString(this ulong self, int radix) => (radix == 16 ? "0x" : (radix == 10 ? "0d" : "0b")) + (radix == 10 ? self.ToString() : Convert.ToString((long)self, radix).ToUpper());
	}
}
