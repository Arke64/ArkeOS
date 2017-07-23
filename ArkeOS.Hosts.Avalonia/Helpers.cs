using Avalonia.Input;

namespace ArkeOS.Hosts.Avalonia {
    public class AvaloniaHelpers {
        public static ulong ConvertFromAvaloniaKey(Key key) {
            switch (key) {
                case Key.OemTilde: return 0x00; // `
                case Key.D1: return 0x01; // 1
                case Key.D2: return 0x02; // 2
                case Key.D3: return 0x03; // 3
                case Key.D4: return 0x04; // 4
                case Key.D5: return 0x05; // 5
                case Key.D6: return 0x06; // 6
                case Key.D7: return 0x07; // 7
                case Key.D8: return 0x08; // 8
                case Key.D9: return 0x09; // 9
                case Key.D0: return 0x0A; // 0
                case Key.OemMinus: return 0x0B; // -
                case Key.OemPlus: return 0x0C; // =
                case Key.Back: return 0x0D; // BACKSPACE
                case Key.Tab: return 0x0E; // TAB
                case Key.Q: return 0x0F; // Q
                case Key.W: return 0x10; // W
                case Key.E: return 0x11; // E
                case Key.R: return 0x12; // R
                case Key.T: return 0x13; // T
                case Key.Y: return 0x14; // Y
                case Key.U: return 0x15; // U
                case Key.I: return 0x16; // I
                case Key.O: return 0x17; // O
                case Key.P: return 0x18; // P
                case Key.OemOpenBrackets: return 0x19; // [
                case Key.OemCloseBrackets: return 0x1A; // ]
                case Key.OemBackslash: return 0x1B; // \
                case Key.CapsLock: return 0x1C; // CAPS LOCK
                case Key.A: return 0x1D; // A
                case Key.S: return 0x1E; // S
                case Key.D: return 0x1F; // D
                case Key.F: return 0x20; // F
                case Key.G: return 0x21; // G
                case Key.H: return 0x22; // H
                case Key.J: return 0x23; // J
                case Key.K: return 0x24; // K
                case Key.L: return 0x25; // L
                case Key.OemSemicolon: return 0x26; // ;
                case Key.OemQuotes: return 0x27; // '
                case Key.Enter: return 0x28; // ENTER
                case Key.LeftShift: return 0x29; // LEFT SHIFT
                case Key.Z: return 0x2A; // Z
                case Key.X: return 0x2B; // X
                case Key.C: return 0x2C; // C
                case Key.V: return 0x2D; // V
                case Key.B: return 0x2E; // B
                case Key.N: return 0x2F; // N
                case Key.M: return 0x30; // M
                case Key.OemComma: return 0x31; // ,
                case Key.OemPeriod: return 0x32; // .
                case Key.OemQuestion: return 0x33; // /
                case Key.RightShift: return 0x34; // RIGHT SHIFT
                case Key.LeftCtrl: return 0x35; // LEFT CONTROL
                //case Key.: return 0x36; // OPTION
                case Key.LeftAlt: return 0x37; // LEFT ALT
                case Key.Space: return 0x38; // SPACE

                case Key.Apps: return 0x3A; // MENU

                case Key.Escape: return 0x3C; // ESCAPE
                case Key.F1: return 0x3D; // F1
                case Key.F2: return 0x3E; // F2
                case Key.F3: return 0x3F; // F3
                case Key.F4: return 0x40; // F4
                case Key.F5: return 0x41; // F5
                case Key.F6: return 0x42; // F6
                case Key.F7: return 0x43; // F7
                case Key.F8: return 0x44; // F8
                case Key.F9: return 0x45; // F9
                case Key.F10: return 0x46; // F10
                case Key.F11: return 0x47; // F11
                case Key.F12: return 0x48; // F12

                case Key.Scroll: return 0x4A; // SCROLL LOCK

                case Key.Multiply: return 0x58; // NUMERIC *
                case Key.Subtract: return 0x59; // NUMERIC -
                case Key.Add: return 0x5A; // NUMERIC +

                case Key.Decimal: return 0x5C; // NUMERIC.
                case Key.NumPad0: return 0x5D; // NUMERIC 0
                case Key.NumPad1: return 0x5E; // NUMERIC 1
                case Key.NumPad2: return 0x5F; // NUMERIC 2
                case Key.NumPad3: return 0x60; // NUMERIC 3
                case Key.NumPad4: return 0x61; // NUMERIC 4
                case Key.NumPad5: return 0x62; // NUMERIC 5
                case Key.NumPad6: return 0x63; // NUMERIC 6
                case Key.NumPad7: return 0x64; // NUMERIC 7
                case Key.NumPad8: return 0x65; // NUMERIC 8
                case Key.NumPad9: return 0x66; // NUMERIC 9
                case Key.RightAlt: return 0x39; // RIGHT ALT

                //case Key.RightCtrl: return 0x3B; // RIGHT CONTROL

                case Key.PrintScreen: return 0x49; // PRINT SCREEN

                case Key.Pause: return 0x4B; // PAUSE BREAK
                case Key.Insert: return 0x4C; // INSERT
                case Key.Delete: return 0x4D; // DELETE
                case Key.Home: return 0x4E; // HOME
                case Key.End: return 0x4F; // END
                case Key.PageUp: return 0x50; // PAGE UP
                case Key.PageDown: return 0x51; // PAGE DOWN
                case Key.Up: return 0x52; // UP
                case Key.Down: return 0x53; // DOWN
                case Key.Left: return 0x54; // LEFT
                case Key.Right: return 0x55; // RIGHT
                case Key.NumLock: return 0x56; // NUM LOCK
                case Key.Divide: return 0x57; // NUMERIC /

                case Key.Separator: return 0x5B; // NUMERIC ENTER

                default: return 0;
            }
        }
    }
}