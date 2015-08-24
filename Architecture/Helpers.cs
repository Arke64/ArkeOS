using System;

namespace ArkeOS.Architecture {
    public static class Helpers {
        public static ulong ParseLiteral(string value) {
            var prefix = value.Substring(0, 2);

            value = value.Substring(2);

            if (prefix == "0x") {
                return Convert.ToUInt64(value, 16);
            }
            else if (prefix == "0d") {
                return Convert.ToUInt64(value, 10);
            }
            else if (prefix == "0o") {
                return Convert.ToUInt64(value, 8);
            }
            else if (prefix == "0b") {
                return Convert.ToUInt64(value, 2);
            }
            else {
                return 0;
            }
        }

        public static T ParseEnum<T>(string value) {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static ulong[] ConvertArray(byte[] buffer) {
            var result = new ulong[buffer.Length / 8];

            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);

            return result;
        }

        public static byte[] ConvertArray(ulong[] buffer) {
            var result = new byte[buffer.Length * 8];

            Buffer.BlockCopy(buffer, 0, result, 0, result.Length);

            return result;
        }
    }
}
