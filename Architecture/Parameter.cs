namespace ArkeOS.Architecture {
    public class Parameter {
        public class Calculated {
            public Parameter Parameter { get; set; }
            public bool IsPositive { get; set; }

            public Calculated(Parameter parameter, bool isPositive) {
                this.Parameter = parameter;
                this.IsPositive = isPositive;
            }
        }

        public ParameterType Type { get; set; }
        public Register Register { get; set; }
        public ulong Address { get; set; }
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
                else if (this.Type == ParameterType.Address) {
                    return 1;
                }
                else {
                    return 0;
                }
            }
        }

        public static Parameter CreateStack(bool isIndirect, bool isRIPRelative) {
            return new Parameter() {
                IsIndirect = isIndirect,
                IsRIPRelative = isRIPRelative,
                Type = ParameterType.Stack,
            };
        }

        public static Parameter CreateRegister(bool isIndirect, bool isRIPRelative, Register register) {
            return new Parameter() {
                IsIndirect = isIndirect,
                IsRIPRelative = isRIPRelative,
                Type = ParameterType.Register,
                Register = register,
            };
        }

        public static Parameter CreateAddress(bool isIndirect, bool isRIPRelative, ulong address) {
            return new Parameter() {
                IsIndirect = isIndirect,
                IsRIPRelative = isRIPRelative,
                Type = ParameterType.Address,
                Address = address,
            };
        }

        public static Parameter CreateCalculated(bool isIndirect, bool isRIPRelative, Calculated @base, Calculated index, Calculated scale, Calculated offset) {
            return new Parameter() {
                Base = @base,
                Index = index,
                Scale = scale,
                Offset = offset,

                IsIndirect = isIndirect,
                IsRIPRelative = isRIPRelative,
                Type = ParameterType.Calculated,
            };
        }

        public override string ToString() {
            var str = "";

            switch (this.Type) {
                default: return string.Empty;
                case ParameterType.Address: str = "0x" + this.Address.ToString("X8"); break;
                case ParameterType.Register: str = this.Register.ToString(); break;
                case ParameterType.Stack: str = "S"; break;
                case ParameterType.Calculated:
                    str = this.Base.Parameter.ToString();

                    if (this.Index != null)
                        str += (this.Index.IsPositive ? " + " : " - ") + this.Index.Parameter.ToString();

                    if (this.Scale != null)
                        str += " * " + this.Scale.Parameter.ToString();

                    if (this.Offset != null)
                        str += (this.Offset.IsPositive ? " + " : " - ") + this.Offset.Parameter.ToString();

                    str = "(" + str + ")";

                    break;
            }

            str = this.IsRIPRelative ? "(" + str + " + RIP)" : str;

            return this.IsIndirect ? "[" + str + "]" : str;
        }
    }
}