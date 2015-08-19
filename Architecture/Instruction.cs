using System;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Architecture {
	public class Instruction {
		public InstructionDefinition Definition { get; private set; }
		public byte Code { get; }
		public byte Length { get; }

		public Parameter Parameter1 { get; private set; }
		public Parameter Parameter2 { get; private set; }
		public Parameter Parameter3 { get; private set; }

		public Instruction(byte code, IList<Parameter> parameters) {
			this.Code = code;
			this.Length = 3;

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
			var paraDef = BitConverter.ToUInt16(memory, (int)(address + 1));

			this.Code = memory[address];
			this.Length = 3;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = Parameter.CreateFromMemory((ParameterType)((paraDef >> 2) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = Parameter.CreateFromMemory((ParameterType)((paraDef >> 5) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = Parameter.CreateFromMemory((ParameterType)((paraDef >> 8) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter3.Length;
			}
		}

		public void Encode(BinaryWriter writer) {
			writer.Write(this.Code);
			writer.Write((ushort)((((byte?)this.Parameter1?.Type ?? 0) << 2) | (((byte?)this.Parameter2?.Type ?? 0) << 5) | (((byte?)this.Parameter3?.Type ?? 0) << 8)));

			this.Parameter1?.Encode(writer);
			this.Parameter2?.Encode(writer);
			this.Parameter3?.Encode(writer);
		}

		public override string ToString() {
			return this.Definition.Mnemonic + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
		}
	}
}