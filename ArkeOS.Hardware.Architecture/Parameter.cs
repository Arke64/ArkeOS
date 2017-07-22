using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Hardware.Architecture {
    public class Parameter {
        public static ulong MaxForEmbeddedLiteral => 255;

        public ParameterType Type { get; set; }
        public Register Register { get; set; }
        public ulong Literal { get; set; }
        public bool IsIndirect { get; set; }
        public ParameterRelativeTo RelativeTo { get; set; }
        public bool ForbidEmbedded { get; set; }

        public byte Length => (byte)(this.Type == ParameterType.Literal && (this.Literal > Parameter.MaxForEmbeddedLiteral || this.ForbidEmbedded) ? 1 : 0);

        public static Parameter CreateStack() => Parameter.CreateStack(ParameterFlags.None);
        public static Parameter CreateRegister(Register register) => Parameter.CreateRegister(register, ParameterFlags.None);
        public static Parameter CreateLiteral(ulong literal) => Parameter.CreateLiteral(literal, ParameterFlags.None);

        public static Parameter CreateStack(ParameterFlags flags) => Parameter.CreateStack((flags & ParameterFlags.Indirect) != 0, (ParameterRelativeTo)(((byte)flags & 0b0110) >> 1));
        public static Parameter CreateRegister(Register register, ParameterFlags flags) => Parameter.CreateRegister(register, (flags & ParameterFlags.Indirect) != 0, (ParameterRelativeTo)(((byte)flags & 0b0110) >> 1));
        public static Parameter CreateLiteral(ulong literal, ParameterFlags flags) => Parameter.CreateLiteral(literal, (flags & ParameterFlags.Indirect) != 0, (ParameterRelativeTo)(((byte)flags & 0b0110) >> 1), (flags & ParameterFlags.ForbidEmbedded) != 0);

        public static Parameter CreateStack(bool isIndirect, ParameterRelativeTo relativeTo) => new Parameter() {
            IsIndirect = isIndirect,
            RelativeTo = relativeTo,
            Type = ParameterType.Stack,
        };

        public static Parameter CreateRegister(Register register, bool isIndirect, ParameterRelativeTo relativeTo) => new Parameter() {
            IsIndirect = isIndirect,
            RelativeTo = relativeTo,
            Type = ParameterType.Register,
            Register = register,
        };

        public static Parameter CreateLiteral(ulong literal, bool isIndirect, ParameterRelativeTo relativeTo) => new Parameter() {
            IsIndirect = isIndirect,
            RelativeTo = relativeTo,
            Type = ParameterType.Literal,
            Literal = literal,
            ForbidEmbedded = false,
        };

        public static Parameter CreateLiteral(ulong literal, bool isIndirect, ParameterRelativeTo relativeTo, bool forbidEmbedded) => new Parameter() {
            IsIndirect = isIndirect,
            RelativeTo = relativeTo,
            Type = ParameterType.Literal,
            Literal = literal,
            ForbidEmbedded = forbidEmbedded,
        };

        public override string ToString() => this.ToString(16);

        public string ToString(int radix) {
            var str = "";

            switch (this.Type) {
                default: return string.Empty;
                case ParameterType.Literal: str = this.Literal.ToString(radix); break;
                case ParameterType.Register: str = this.Register.ToString(); break;
                case ParameterType.Stack: str = "S"; break;
            }

            switch (this.RelativeTo) {
                case ParameterRelativeTo.RIP: str = "{" + str + "}"; break;
                case ParameterRelativeTo.RSP: str = "<" + str + ">"; break;
                case ParameterRelativeTo.RBP: str = "(" + str + ")"; break;
            }

            return this.IsIndirect ? "[" + str + "]" : str;
        }
    }
}
