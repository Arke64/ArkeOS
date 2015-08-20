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
		public ulong Literal { get; private set; }
		public byte Length { get; private set; }

		public Calculated Base { get; private set; }
		public Calculated Index { get; private set; }
		public Calculated Scale { get; private set; }
		public Calculated Offset { get; private set; }

		private Parameter() {

		}

		public static Parameter CreateStack(bool isAddress) {
			return new Parameter() {
				Type = isAddress ? ParameterType.StackAddress : ParameterType.Stack,
				Length = 0
			};
		}

		public static Parameter CreateRegister(bool isAddress, Register register) {
			return new Parameter() {
				Type = isAddress ? ParameterType.RegisterAddress : ParameterType.Register,
				Register = register,
				Length = 0
			};
		}

		public static Parameter CreateLiteral(bool isAddress, ulong literal) {
			return new Parameter() {
				Type = isAddress ? ParameterType.LiteralAddress : ParameterType.Literal,
				Literal = literal,
				Length = 1
			};
		}

		public static Parameter CreateCalculated(bool isAddress, Calculated @base, Calculated index, Calculated scale, Calculated offset) {
			return new Parameter() {
				Base = @base,
				Index = index,
				Scale = scale,
				Offset = offset,

				Type = isAddress ? ParameterType.CalculatedAddress : ParameterType.Calculated,
				Length = (byte)(1 + @base.Parameter.Length + index.Parameter.Length + (scale?.Parameter.Length ?? 0) + (offset?.Parameter.Length ?? 0))
			};
		}

		public override string ToString() {
			switch (this.Type) {
				default: return string.Empty;
				case ParameterType.Literal: return this.ParameterToString();
				case ParameterType.LiteralAddress: return $"[0x{this.ParameterToString()}]";
				case ParameterType.Register: return this.ParameterToString();
				case ParameterType.RegisterAddress: return $"[{this.ParameterToString()}]";
				case ParameterType.Stack: return "S";
				case ParameterType.StackAddress: return "(S)";
				case ParameterType.Calculated:
				case ParameterType.CalculatedAddress:
					var str = string.Empty;

					str += this.Base.Parameter.ToString();

					if (this.Index != null)
						str += (this.Index.IsPositive ? " + " : " - ") + this.Index.Parameter.ToString();

					if (this.Scale != null)
						str += " * " + this.Scale.Parameter.ToString();

					if (this.Offset != null)
						str += (this.Offset.IsPositive ? " + " : " - ") + this.Offset.Parameter.ToString();

					return this.Type == ParameterType.CalculatedAddress ? "[(" + str + ")]" : "(" + str + ")";
			}
		}

		private string ParameterToString() {
			switch (this.Type) {
				case ParameterType.Literal: return "0x" + this.Literal.ToString("X8");
				case ParameterType.LiteralAddress: return "0x" + this.Literal.ToString("X8");
				case ParameterType.Register: return this.Register.ToString();
				case ParameterType.RegisterAddress: return this.Register.ToString();
				default: return string.Empty;
			}
		}
	}
}