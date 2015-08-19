using System;

namespace ArkeOS.Architecture {
	public static class Helpers {
		public static ulong ParseLiteral(string value) {
			if (value.IndexOf("0x") == 0) {
				return Convert.ToUInt64(value.Substring(2), 16);
			}
			else if (value.IndexOf("0d") == 0) {
				return Convert.ToUInt64(value.Substring(2), 10);
			}
			else if (value.IndexOf("0o") == 0) {
				return Convert.ToUInt64(value.Substring(2), 8);
			}
			else if (value.IndexOf("0b") == 0) {
				return Convert.ToUInt64(value.Substring(2), 2);
			}
			else {
				return 0;
			}
		}

		public static T ParseEnum<T>(string value) {
			return (T)Enum.Parse(typeof(T), value);
		}
	}
}
