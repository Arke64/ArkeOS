using System;
using System.IO;
using System.Text.RegularExpressions;

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
		public static string ToString(this ulong self, int radix) => (radix == 16 ? "0x" : (radix == 10 ? "0d" : "0b")) + (radix == 10 ? self.ToString() : Convert.ToString((long)self, radix).ToUpper()).InsertDivider('_', radix == 10 ? 3 : 4);
	}

    public static class StringExtensions {
        public static string InsertDivider(this string self, char divider, int chunkSize) => Regex.Replace(self.PadLeft(self.Length + (chunkSize - self.Length % chunkSize), '0'), ".{" + chunkSize + "}", "$0" + divider).TrimStart('0').TrimEnd(divider).PadLeft(1, '0');
    }
}
