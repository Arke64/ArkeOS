using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Architecture {
	public class Instruction {
		public InstructionDefinition Definition { get; private set; }
		public byte Code { get; }
		public InstructionSize Size { get; }
		public byte Length { get; }

		public Parameter Parameter1 { get; private set; }
		public Parameter Parameter2 { get; private set; }
		public Parameter Parameter3 { get; private set; }

		public byte SizeInBytes => Helpers.SizeToBytes(this.Size);
		public byte SizeInBits => Helpers.SizeToBits(this.Size);
		public ulong SizeMask => Helpers.SizeToMask(this.Size);

		public Instruction(byte code, InstructionSize size, IList<Parameter> parameters) {
			this.Code = code;
			this.Size = size;
			this.Length = 2;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = parameters[0];
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = parameters[1];
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = parameters[2];
				this.Length += this.Parameter3.Length;
			}
		}

		public Instruction(byte[] memory, ulong address) {
			var b1 = memory[address];
			var b2 = memory[address + 1];

			this.Size = (InstructionSize)(b1 & 0x03);
			this.Code = (byte)((b1 & 0xFC) >> 2);
			this.Length = 2;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = new Parameter((ParameterType)((b2 >> 0) & 0x03), this.Size, memory, address + this.Length);
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = new Parameter((ParameterType)((b2 >> 2) & 0x03), this.Size, memory, address + this.Length);
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = new Parameter((ParameterType)((b2 >> 4) & 0x03), this.Size, memory, address + this.Length);
				this.Length += this.Parameter3.Length;
			}
		}

		public void Encode(BinaryWriter writer) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));
			writer.Write((byte)((byte)this.Parameter1?.Type << 0 | (byte)this.Parameter2?.Type << 2 | (byte)this.Parameter3?.Type << 4));

			this.Parameter1?.Encode(writer);
			this.Parameter2?.Encode(writer);
			this.Parameter3?.Encode(writer);
		}

		public override string ToString() {
			return this.Definition.Mnemonic + ":" + this.SizeInBytes + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
		}
	}
}