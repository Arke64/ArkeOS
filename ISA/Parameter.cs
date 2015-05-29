using System;
using System.IO;

namespace ArkeOS.ISA {
	public class Parameter {
		public InstructionSize Size { get; set; }
		public ParameterType Type { get; set; }
		public Register Register { get; set; }
		public ulong Literal { get; set; }
		public string Label { get; set; }
		public byte Length { get; set; }

		public Parameter(InstructionSize size, ParameterType type, ulong value) {
			this.Size = size;
			this.Type = type;
			this.Label = string.Empty;

			switch (type) {
				case ParameterType.Register:
					this.Register = (Register)value;
					this.Length = 1;

					break;

				case ParameterType.RegisterAddress:
					this.Register = (Register)value;
					this.Length = 1;

					break;

				case ParameterType.Literal:
					this.Literal = value & Instruction.SizeToMask(size);
					this.Length = Instruction.SizeToBytes(size);

					break;

				case ParameterType.LiteralAddress:
					this.Literal = value;
					this.Length = 8;

					break;
			}
		}

		public Parameter(InstructionSize size, string value) {
			this.Size = size;

			if (value[0] == '0') {
				this.Literal = Parameter.ParseLiteral(value);
				this.Type = ParameterType.Literal;
				this.Length = Instruction.SizeToBytes(size);
			}
			else if (value[0] == 'R') {
				this.Register = (Register)Enum.Parse(typeof(Register), value);
				this.Type = ParameterType.Register;
				this.Length = 1;
			}
			else if (value[0] == '{') {
				this.Label = value.Substring(1, value.Length - 2).Trim();
				this.Type = ParameterType.Label;
				this.Length = 8;
			}
			else if (value[0] == '[') {
				value = value.Substring(1, value.Length - 2).Trim();

				if (value[0] == '0') {
					this.Literal = Parameter.ParseLiteral(value);
					this.Type = ParameterType.LiteralAddress;
					this.Length = 8;
				}
				else if (value[0] == 'R') {
					this.Register = (Register)Enum.Parse(typeof(Register), value);
					this.Type = ParameterType.RegisterAddress;
					this.Length = 1;
				}
			}
		}

		public void Serialize(BinaryWriter writer) {
			switch (this.Type) {
				case ParameterType.RegisterAddress: writer.Write((byte)this.Register); break;
				case ParameterType.Register: writer.Write((byte)this.Register); break;
				case ParameterType.LiteralAddress: writer.Write(this.Literal); break;
				case ParameterType.Literal:
					switch (this.Size) {
						case InstructionSize.OneByte: writer.Write((byte)this.Literal); break;
						case InstructionSize.TwoByte: writer.Write((ushort)this.Literal); break;
						case InstructionSize.FourByte: writer.Write((uint)this.Literal); break;
						case InstructionSize.EightByte: writer.Write(this.Literal); break;
					}

					break;
			}
		}

		public override string ToString() {
			switch (this.Type) {
				case ParameterType.Literal: return "0x" + this.Literal.ToString("X8");
				case ParameterType.LiteralAddress: return $"[0x{this.Literal.ToString("X8")}]";
				case ParameterType.Register: return this.Register.ToString();
				case ParameterType.RegisterAddress: return $"[{this.Register.ToString()}]";
				case ParameterType.Label: return $"{{{this.Label}}}";
				default: return string.Empty;
			}
		}

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
	}
}