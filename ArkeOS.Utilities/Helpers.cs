using System;

namespace ArkeOS.Utilities {
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
			else if (prefix == "0c") {
				return value[0];
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

		public static ulong ConvertFromWindowsScanCode(bool isExtended, uint code) {
			if (!isExtended) {
				switch (code) {
					case 0x29: return 0x00; // `
					case 0x02: return 0x01; // 1
					case 0x03: return 0x02; // 2
					case 0x04: return 0x03; // 3
					case 0x05: return 0x04; // 4
					case 0x06: return 0x05; // 5
					case 0x07: return 0x06; // 6
					case 0x08: return 0x07; // 7
					case 0x09: return 0x08; // 8
					case 0x0A: return 0x09; // 9
					case 0x0B: return 0x0A; // 0
					case 0x0C: return 0x0B; // -
					case 0x0D: return 0x0C; // =
					case 0x0E: return 0x0D; // BACKSPACE
					case 0x0F: return 0x0E; // TAB
					case 0x10: return 0x0F; // Q
					case 0x11: return 0x10; // W
					case 0x12: return 0x11; // E
					case 0x13: return 0x12; // R
					case 0x14: return 0x13; // T
					case 0x15: return 0x14; // Y
					case 0x16: return 0x15; // U
					case 0x17: return 0x16; // I
					case 0x18: return 0x17; // O
					case 0x19: return 0x18; // P
					case 0x1A: return 0x19; // [
					case 0x1B: return 0x1A; // ]
					case 0x2B: return 0x1B; // \
					case 0x3A: return 0x1C; // CAPS LOCK
					case 0x1E: return 0x1D; // A
					case 0x1F: return 0x1E; // S
					case 0x20: return 0x1F; // D
					case 0x21: return 0x20; // F
					case 0x22: return 0x21; // G
					case 0x23: return 0x22; // H
					case 0x24: return 0x23; // J
					case 0x25: return 0x24; // K
					case 0x26: return 0x25; // L
					case 0x27: return 0x26; // ;
					case 0x28: return 0x27; // '
					case 0x1C: return 0x28; // ENTER
					case 0x2A: return 0x29; // LEFT SHIFT
					case 0x2C: return 0x2A; // Z
					case 0x2D: return 0x2B; // X
					case 0x2E: return 0x2C; // C
					case 0x2F: return 0x2D; // V
					case 0x30: return 0x2E; // B
					case 0x31: return 0x2F; // N
					case 0x32: return 0x30; // M
					case 0x33: return 0x31; // ,
					case 0x34: return 0x32; // .
					case 0x35: return 0x33; // /
					case 0x36: return 0x34; // RIGHT SHIFT
					case 0x1D: return 0x35; // LEFT CONTROL
					case 0x5B: return 0x36; // OPTION
					case 0x38: return 0x37; // LEFT ALT
					case 0x39: return 0x38; // SPACE
					case 0x5D: return 0x3A; // MENU
					default: return 0;
				}
			}
			else {
				switch (code) {
					case 0x38: return 0x39; // RIGHT ALT
					case 0x1D: return 0x3B; // RIGHT CONTROL
					default: return 0;
				}
			}
		}
	}
}
