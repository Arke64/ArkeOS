using System;

namespace ArkeOS.Utilities {
    public static class Helpers {
        public static ulong ParseLiteral(string value) {
            var prefix = value.Substring(0, 2);

            value = value.Substring(2).Replace("_", string.Empty);

            switch (prefix) {
                case "0x": return Convert.ToUInt64(value, 16);
                case "0d": return Convert.ToUInt64(value, 10);
                case "0o": return Convert.ToUInt64(value, 8);
                case "0b": return Convert.ToUInt64(value, 2);
                case "0c": return value[0];
                default: return 0;
            }
        }

        public static T ParseEnum<T>(string value) => (T)Enum.Parse(typeof(T), value);

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

                    case 0x01: return 0x3C; // ESCAPE
                    case 0x3B: return 0x3D; // F1
                    case 0x3C: return 0x3E; // F2
                    case 0x3D: return 0x3F; // F3
                    case 0x3E: return 0x40; // F4
                    case 0x3F: return 0x41; // F5
                    case 0x40: return 0x42; // F6
                    case 0x41: return 0x43; // F7
                    case 0x42: return 0x44; // F8
                    case 0x43: return 0x45; // F9
                    case 0x44: return 0x46; // F10
                    case 0x57: return 0x47; // F11
                    case 0x58: return 0x48; // F12

                    case 0x46: return 0x4A; // SCROLL LOCK

                    case 0x37: return 0x58; // NUMERIC *
                    case 0x4A: return 0x59; // NUMERIC -
                    case 0x4E: return 0x5A; // NUMERIC +

                    case 0x53: return 0x5C; // NUMERIC.
                    case 0x52: return 0x5D; // NUMERIC 0
                    case 0x4F: return 0x5E; // NUMERIC 1
                    case 0x50: return 0x5F; // NUMERIC 2
                    case 0x51: return 0x60; // NUMERIC 3
                    case 0x4B: return 0x61; // NUMERIC 4
                    case 0x4C: return 0x62; // NUMERIC 5
                    case 0x4D: return 0x63; // NUMERIC 6
                    case 0x47: return 0x64; // NUMERIC 7
                    case 0x48: return 0x65; // NUMERIC 8
                    case 0x49: return 0x66; // NUMERIC 9

                    default: return 0;
                }
            }
            else {
                switch (code) {
                    case 0x38: return 0x39; // RIGHT ALT

                    //case 0x1D: return 0x3B; // RIGHT CONTROL

                    case 0x37: return 0x49; // PRINT SCREEN

                    case 0x1D: return 0x4B; // PAUSE BREAK
                    case 0x52: return 0x4C; // INSERT
                    case 0x53: return 0x4D; // DELETE
                    case 0x47: return 0x4E; // HOME
                    case 0x4F: return 0x4F; // END
                    case 0x49: return 0x50; // PAGE UP
                    case 0x51: return 0x51; // PAGE DOWN
                    case 0x48: return 0x52; // UP
                    case 0x50: return 0x53; // DOWN
                    case 0x4B: return 0x54; // LEFT
                    case 0x4D: return 0x55; // RIGHT
                    case 0x45: return 0x56; // NUM LOCK
                    case 0x35: return 0x57; // NUMERIC /

                    case 0x1C: return 0x5B; // NUMERIC ENTER

                    default: return 0;
                }
            }
        }
    }
}
