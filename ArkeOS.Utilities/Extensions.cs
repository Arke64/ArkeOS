using System;
using System.IO;
using System.Linq;

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
        public static string ToString(this ulong self, int radix) {
            var prefix = radix == 16 ? "0x" :
                         radix == 10 ? "0d" :
                         radix == 2 ? "0b" :
                         throw new ArgumentException("Invalid radix.", nameof(radix));

            var str = Convert.ToString((long)self, radix).ToUpper();
            var chunkSize = radix == 10 ? 3 : 4;
            var formattedLength = str.Length % chunkSize == 0 ? str.Length : (str.Length / chunkSize + 1) * chunkSize;

            str = str.PadLeft(formattedLength, '0');

            return prefix + string.Join("_", Enumerable.Range(0, str.Length / chunkSize).Select(i => str.Substring(i * chunkSize, chunkSize)));
        }
    }
}
