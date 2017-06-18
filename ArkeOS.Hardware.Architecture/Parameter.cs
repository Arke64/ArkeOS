using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Hardware.Architecture {
    public class Parameter {
        public class Calculated {
            public bool IsPositive { get; set; }
            public Parameter Parameter { get; set; }

            public Calculated(bool isPositive, Parameter parameter) {
                this.IsPositive = isPositive;
                this.Parameter = parameter;
            }
        }

        public ParameterType Type { get; set; }
        public Register Register { get; set; }
        public ulong Literal { get; set; }
        public bool IsIndirect { get; set; }
        public bool IsRIPRelative { get; set; }

        public Calculated Base { get; set; }
        public Calculated Index { get; set; }
        public Calculated Scale { get; set; }
        public Calculated Offset { get; set; }

        public byte Length {
            get {
                if (this.Type == ParameterType.Calculated) {
                    return (byte)(1 + this.Base.Parameter.Length + (this.Index?.Parameter.Length ?? 0) + (this.Scale?.Parameter.Length ?? 0) + (this.Offset?.Parameter.Length ?? 0));
                }
                else if (this.Type == ParameterType.Literal) {
                    return 1;
                }
                else {
                    return 0;
                }
            }
        }

        public static Parameter CreateStack(bool isIndirect, bool isRIPRelative) => new Parameter() {
            IsIndirect = isIndirect,
            IsRIPRelative = isRIPRelative,
            Type = ParameterType.Stack,
        };

        public static Parameter CreateRegister(Register register, bool isIndirect, bool isRIPRelative) => new Parameter() {
            IsIndirect = isIndirect,
            IsRIPRelative = isRIPRelative,
            Type = ParameterType.Register,
            Register = register,
        };

        public static Parameter CreateLiteral(ulong literal, bool isIndirect, bool isRIPRelative) => new Parameter() {
            IsIndirect = isIndirect,
            IsRIPRelative = isRIPRelative,
            Type = ParameterType.Literal,
            Literal = literal,
        };

        public static Parameter CreateCalculated(Calculated @base, Calculated index, Calculated scale, Calculated offset, bool isIndirect, bool isRIPRelative) => new Parameter() {
            Base = @base,
            Index = index,
            Scale = scale,
            Offset = offset,

            IsIndirect = isIndirect,
            IsRIPRelative = isRIPRelative,
            Type = ParameterType.Calculated,
        };

        public override string ToString() => this.ToString(16);

        public string ToString(int radix) {
            var str = "";

            switch (this.Type) {
                default: return string.Empty;
                case ParameterType.Literal: str = this.Literal.ToString(radix); break;
                case ParameterType.Register: str = this.Register.ToString(); break;
                case ParameterType.Stack: str = "S"; break;
                case ParameterType.Calculated:
                    str = this.Base.Parameter.ToString(radix);
                    str += (this.Index.IsPositive ? " + " : " + -") + this.Index.Parameter.ToString(radix);
                    str += (this.Scale.IsPositive ? " * " : " * -") + this.Scale.Parameter.ToString(radix);
                    str += (this.Offset.IsPositive ? " + " : " + -") + this.Offset.Parameter.ToString(radix);

                    str = "(" + str + ")";

                    break;
            }

            str = this.IsRIPRelative ? "{" + str + "}" : str;

            return this.IsIndirect ? "[" + str + "]" : str;
        }
    }
}
