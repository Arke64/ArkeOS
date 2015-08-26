namespace ArkeOS.Architecture {
    public class Parameter {
        public class Calculated {
            public Parameter Parameter { get; private set; }
            public bool IsPositive { get; private set; }

            public Calculated(Parameter parameter, bool isPositive) {
                this.Parameter = parameter;
                this.IsPositive = isPositive;
            }
        }

        public ParameterType Type { get; private set; }
        public Register Register { get; private set; }
        public ulong Address { get; private set; }
        public byte Length { get; private set; }
        public bool IsIndirect { get; private set; }

        public Calculated Base { get; private set; }
        public Calculated Index { get; private set; }
        public Calculated Scale { get; private set; }
        public Calculated Offset { get; private set; }

        private Parameter() {

        }

        public static Parameter CreateStack(bool isIndirect) {
            return new Parameter() {
                IsIndirect = isIndirect,
                Type = ParameterType.Stack,
                Length = 0
            };
        }

        public static Parameter CreateRegister(bool isIndirect, Register register) {
            return new Parameter() {
                IsIndirect = isIndirect,
                Type = ParameterType.Register,
                Register = register,
                Length = 0
            };
        }

        public static Parameter CreateAddress(bool isIndirect, ulong address) {
            return new Parameter() {
                IsIndirect = isIndirect,
                Type = ParameterType.Address,
                Address = address,
                Length = 1
            };
        }

        public static Parameter CreateCalculated(bool isIndirect, Calculated @base, Calculated index, Calculated scale, Calculated offset) {
            return new Parameter() {
                Base = @base,
                Index = index,
                Scale = scale,
                Offset = offset,

                IsIndirect = isIndirect,
                Type = ParameterType.Calculated,
                Length = (byte)(1 + @base.Parameter.Length + (index?.Parameter.Length ?? 0) + (scale?.Parameter.Length ?? 0) + (offset?.Parameter.Length ?? 0))
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

            return this.IsIndirect ? "[" + str + "]" : str;
        }
    }
}