using System;
using System.IO;

namespace ArkeOS.Executable {
	public class Parameter {
		public ParameterType Type { get; private set; }
		public Register Register { get; private set; }
		public ulong Literal { get; private set; }
		public byte Length { get; private set; }
		public string Label { get; private set; }

		public Parameter(InstructionSize size, ParameterType type, ulong value) {
			this.Type = type;

			switch (type) {
				case ParameterType.Register:
					this.Register = (Register)value;
					this.Length = 1;

					break;

				case ParameterType.Literal:
					this.Literal = value;

					switch (size) {
						case InstructionSize.OneByte:
							this.Length = 1;
							this.Literal = (byte)value;

							break;

						case InstructionSize.TwoByte:
							this.Length = 2;
							this.Literal = (ushort)value;

							break;

						case InstructionSize.FourByte:
							this.Length = 4;
							this.Literal = (uint)value;

							break;

						case InstructionSize.EightByte:
							this.Length = 8;
							this.Literal = value;

							break;
					}

					break;

				case ParameterType.LiteralAddress:
					this.Literal = value;
					this.Length = 8;

					break;

				case ParameterType.RegisterAddress:
					this.Register = (Register)value;
					this.Length = 1;

					break;
			}
		}

		public Parameter(InstructionSize size, string value) {
			if (value[0] == '0') {
				this.Literal = this.ParseLiteral(value);
				this.Type = ParameterType.Literal;
				this.Length = Instruction.SizeToBytes(size);
			}
			else if (value[0] == '[') {
				value = value.Substring(1, value.Length - 2).Trim();

				if (value[0] == '0') {
					this.Literal = this.ParseLiteral(value);
					this.Type = ParameterType.LiteralAddress;
					this.Length = 8;
				}
				else if (value[0] == 'R') {
					this.Register = (Register)Enum.Parse(typeof(Register), value);
					this.Type = ParameterType.RegisterAddress;
					this.Length = 1;
				}
			}
			else if (value[0] == '{') {
				this.Label = value.Substring(1, value.Length - 2).Trim();
				this.Type = ParameterType.Label;
				this.Length = 8;
			}
			else if (value[0] == 'R') {
				this.Register = (Register)Enum.Parse(typeof(Register), value);
				this.Type = ParameterType.Register;
				this.Length = 1;
			}
		}

		public void ResolveLabel(Image parentImage) {
			this.Literal = parentImage.FindByLabel(this.Label).Address;
			this.Type = ParameterType.Literal;
        }

		public void Serialize(BinaryWriter writer, InstructionSize size) {
			switch (this.Type) {
				case ParameterType.Register: writer.Write((byte)this.Register); break;
				case ParameterType.LiteralAddress: writer.Write(this.Literal); break;
				case ParameterType.RegisterAddress: writer.Write((byte)this.Register); break;
				case ParameterType.Literal:
					switch (size) {
						case InstructionSize.OneByte: writer.Write((byte)this.Literal); break;
						case InstructionSize.TwoByte: writer.Write((ushort)this.Literal); break;
						case InstructionSize.FourByte: writer.Write((uint)this.Literal); break;
						case InstructionSize.EightByte: writer.Write(this.Literal); break;
					}

					break;
			}
		}

		private ulong ParseLiteral(string value) {
			if (value.IndexOf("0x") == 0) {
				return Convert.ToUInt64(value.Substring(2), 16);
			}
			else if (value.IndexOf("0d") == 0) {
				return ulong.Parse(value.Substring(2));
			}
			else if (value.IndexOf("0o") == 0) {
				return Convert.ToUInt64(value.Substring(2), 8);
			}
			else if (value.IndexOf("0b") == 0) {
				return Convert.ToUInt64(value.Substring(2), 2);
			}
			else {
				throw new Exception();
			}
		}
	}
}