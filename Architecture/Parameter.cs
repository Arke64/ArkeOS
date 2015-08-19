using System;
using System.IO;

namespace ArkeOS.Architecture {
	public class Parameter {
		public ParameterType Type { get; private set; }
		public Register Register { get; private set; }
		public ulong Literal { get; private set; }
		public byte Length { get; private set; }

		public Parameter CalculatedBase { get; private set; }
		public Parameter CalculatedIndex { get; private set; }
		public Parameter CalculatedScale { get; private set; }
		public Parameter CalculatedOffset { get; private set; }
		public bool CalculatedIndexSign { get; private set; }

		private Parameter() {

		}

		public static Parameter CreateStack(bool isAddress) {
			return new Parameter() {
				Type = isAddress ? ParameterType.StackAddress : ParameterType.StackLiteral,
				Length = 0
			};
		}

		public static Parameter CreateRegister(bool isAddress, Register register) {
			return new Parameter() {
				Type = isAddress ? ParameterType.RegisterAddress : ParameterType.Register,
				Register = register,
				Length = 1
			};
		}

		public static Parameter CreateLiteral(bool isAddress, ulong literal) {
			return new Parameter() {
				Type = isAddress ? ParameterType.LiteralAddress : ParameterType.Literal,
				Literal = literal,
				Length = 8
			};
		}

		public static Parameter CreateCalculated(bool isAddress, Parameter calculatedBase, Parameter calculatedIndex, Parameter calculatedScale, Parameter calculatedOffset, bool calculatedIndexSign) {
			return new Parameter() {
				CalculatedBase = calculatedBase,
				CalculatedIndex = calculatedIndex,
				CalculatedScale = calculatedScale,
				CalculatedOffset = calculatedOffset,
				CalculatedIndexSign = calculatedIndexSign,

				Type = isAddress ? ParameterType.CalculatedAddress : ParameterType.CalculatedLiteral,
				Length = (byte)(1 + calculatedBase.Length + calculatedIndex.Length + (calculatedScale?.Length ?? 0) + (calculatedOffset?.Length ?? 0))
			};
		}

		public static Parameter CreateFromMemory(ParameterType type, byte[] memory, ulong address) {
			var result = new Parameter();

			result.Type = type;

			switch (type) {
				case ParameterType.RegisterAddress: result.Register = (Register)memory[address]; result.Length = 1; break;
				case ParameterType.Register: result.Register = (Register)memory[address]; result.Length = 1; break;
				case ParameterType.LiteralAddress: result.Literal = BitConverter.ToUInt64(memory, (int)address); result.Length = 8; break;
				case ParameterType.StackLiteral: result.Length = 0; break;
				case ParameterType.Literal: result.Literal = BitConverter.ToUInt64(memory, (int)address); result.Length = 8; break;
				case ParameterType.CalculatedAddress:
				case ParameterType.CalculatedLiteral:
					var format = memory[address++];

					result.CalculatedBase = result.ReadCalculatedParameter(memory, ref address, format, -1, 0x08);
					result.CalculatedIndex = result.ReadCalculatedParameter(memory, ref address, format, 0x01, 0x10);
					result.CalculatedScale = result.ReadCalculatedParameter(memory, ref address, format, 0x02, 0x20);
					result.CalculatedOffset = result.ReadCalculatedParameter(memory, ref address, format, 0x04, 0x40);

					result.CalculatedIndexSign = (format & 0x80) != 0;

					result.Length = (byte)(1 + result.CalculatedBase.Length + result.CalculatedIndex.Length + (result.CalculatedScale?.Length ?? 0) + (result.CalculatedOffset?.Length ?? 0));

					break;
			}

			return result;
		}

		public void Encode(BinaryWriter writer) {
			switch (this.Type) {
				case ParameterType.RegisterAddress: writer.Write((byte)this.Register); break;
				case ParameterType.Register: writer.Write((byte)this.Register); break;
				case ParameterType.LiteralAddress: writer.Write(this.Literal); break;
				case ParameterType.Literal: writer.Write(this.Literal); break;
				case ParameterType.StackLiteral: break;
				case ParameterType.CalculatedLiteral:
				case ParameterType.CalculatedAddress:
					byte format = 0;

					format |= (byte)(this.CalculatedBase.Type == ParameterType.Literal ? 0x08 : 0x00);
					format |= (byte)(this.CalculatedIndexSign ? 0x80 : 0x00);

					if (this.CalculatedIndex != null) {
						format |= 0x01;
						format |= (byte)(this.CalculatedIndex.Type == ParameterType.Literal ? 0x10 : 0x00);
					}

					if (this.CalculatedScale != null) {
						format |= 0x02;
						format |= (byte)(this.CalculatedScale.Type == ParameterType.Literal ? 0x20 : 0x00);
					}

					if (this.CalculatedOffset != null) {
						format |= 0x04;
						format |= (byte)(this.CalculatedOffset.Type == ParameterType.Literal ? 0x40 : 0x00);
					}

					writer.Write(format);

					this.CalculatedBase.Encode(writer);
					this.CalculatedIndex.Encode(writer);
					this.CalculatedScale?.Encode(writer);
					this.CalculatedOffset?.Encode(writer);

					break;
			}
		}

		public override string ToString() {
			switch (this.Type) {
				case ParameterType.Literal: return this.ParameterToString();
				case ParameterType.LiteralAddress: return $"[0x{this.ParameterToString()}]";
				case ParameterType.Register: return this.ParameterToString();
				case ParameterType.RegisterAddress: return $"[{this.ParameterToString()}]";
				case ParameterType.StackLiteral: return "S";
				default: return string.Empty;
				case ParameterType.CalculatedLiteral:
				case ParameterType.CalculatedAddress:
					var str = string.Empty;

					str += this.CalculatedBase.ParameterToString();

					if (this.CalculatedIndex != null)
						str += " + " + this.CalculatedIndex.ToString();

					if (this.CalculatedScale != null)
						str += " * " + this.CalculatedScale.ToString();

					if (this.CalculatedOffset != null)
						str += " + " + this.CalculatedOffset.ToString();

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

		private Parameter ReadCalculatedParameter(byte[] memory, ref ulong address, byte format, int hasIndex, int typeIndex) {
			if (hasIndex != -1 && (format & hasIndex) == 0)
				return null;

			if ((format & typeIndex) == 0) {
				return Parameter.CreateRegister(false, (Register)memory[address++]);
			}
			else {
				return Parameter.CreateLiteral(false, BitConverter.ToUInt64(memory, (int)address));
			}
		}
	}
}