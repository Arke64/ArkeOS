using System;
using System.IO;

namespace ArkeOS.Architecture {
	public class Parameter {
		public ParameterType Type { get; }
		public Register Register { get; }
		public ulong Literal { get; }
		public byte Length { get; }

		public Parameter(ParameterType type, Register register) {
			this.Type = type;
			this.Register = register;
			this.Length = 1;
		}

		public Parameter(ParameterType type, ulong literal, byte length) {
			this.Type = type;
			this.Literal = literal;
			this.Length = length;
		}

		public Parameter(ParameterType type, InstructionSize size, byte[] memory, ulong address) {
			this.Type = type;

			switch (type) {
				case ParameterType.RegisterAddress: this.Register = (Register)memory[address]; this.Length = 1; break;
				case ParameterType.Register: this.Register = (Register)memory[address]; this.Length = 1; break;
				case ParameterType.LiteralAddress: this.Literal = BitConverter.ToUInt64(memory, (int)address); this.Length = 8; break;
				case ParameterType.Literal:
					switch (size) {
						case InstructionSize.OneByte: this.Literal = memory[address]; this.Length = 1; break;
						case InstructionSize.TwoByte: this.Literal = BitConverter.ToUInt16(memory, (int)address); this.Length = 2; break;
						case InstructionSize.FourByte: this.Literal = BitConverter.ToUInt32(memory, (int)address); this.Length = 4; break;
						case InstructionSize.EightByte: this.Literal = BitConverter.ToUInt64(memory, (int)address); this.Length = 8; break;
					}

					break;
			}
		}

		public void Encode(BinaryWriter writer) {
			switch (this.Type) {
				case ParameterType.RegisterAddress: writer.Write((byte)this.Register); break;
				case ParameterType.Register: writer.Write((byte)this.Register); break;
				case ParameterType.LiteralAddress: writer.Write(this.Literal); break;
				case ParameterType.Literal: Helpers.SizedWrite(writer, this.Literal, this.Length); break;
			}
		}

		public override string ToString() {
			switch (this.Type) {
				case ParameterType.Literal: return "0x" + this.Literal.ToString("X8");
				case ParameterType.LiteralAddress: return $"[0x{this.Literal.ToString("X8")}]";
				case ParameterType.Register: return this.Register.ToString();
				case ParameterType.RegisterAddress: return $"[{this.Register.ToString()}]";
				default: return string.Empty;
			}
		}
	}
}