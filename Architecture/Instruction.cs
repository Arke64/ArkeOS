using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.Architecture {
	public class Instruction {
		private Parameter[] parameters;
		private InstructionDefinition definition;

		public byte Code { get; }
		public InstructionSize Size { get; }
		public byte Length { get; }

		public Parameter Parameter1 => this.parameters[0];
		public Parameter Parameter2 => this.parameters[1];
		public Parameter Parameter3 => this.parameters[2];
		public Parameter Parameter4 => this.parameters[3];

		public byte SizeInBytes => Helpers.SizeToBytes(this.Size);
		public byte SizeInBits => Helpers.SizeToBits(this.Size);
		public ulong SizeMask => Helpers.SizeToMask(this.Size);

		public Instruction(byte code, InstructionSize size, List<Parameter> parameters) {
			this.Code = code;
			this.Size = size;

			this.definition = InstructionDefinition.Find(this.Code);
			this.parameters = parameters.ToArray();

			this.Length = (byte)(this.parameters.Sum(p => p.Length) + 2);
		}

		public Instruction(byte[] memory, ulong address) {
			var startAddress = address;
			var b1 = memory[address++];
			var b2 = memory[address++];

			this.Size = (InstructionSize)(b1 & 0x03);
			this.Code = (byte)((b1 & 0xFC) >> 2);

			this.definition = InstructionDefinition.Find(this.Code);
			this.parameters = new Parameter[this.definition.ParameterCount];

			for (var i = 0; i < this.definition.ParameterCount; address += this.parameters[i++].Length)
				this.parameters[i] = new Parameter((ParameterType)((b2 >> (i * 2)) & 0x03), this.Size, memory, address);

			this.Length = (byte)(address - startAddress);
		}

		public void Encode(BinaryWriter writer) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));

			byte b = 0;
			for (var i = 0; i < this.definition.ParameterCount; i++)
				b |= (byte)(((byte)this.parameters[i].Type) << (i * 2));

			writer.Write(b);

			foreach (var p in this.parameters)
				p.Encode(writer);
		}

		public override string ToString() {
			return this.definition.Mnemonic + ":" + this.SizeInBytes + " " + string.Join(" ", this.parameters.Select(p => p.ToString()));
		}
	}
}