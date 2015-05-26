using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.ISA {
	public class Instruction {
		private List<Parameter> parameters;

		public ulong Address { get; set; }
		public byte Code { get; }
		public InstructionSize Size { get; }
		public string Label { get; }

		public Parameter Parameter1 => this.parameters[0];
		public Parameter Parameter2 => this.parameters[1];
		public Parameter Parameter3 => this.parameters[2];
		public Parameter Parameter4 => this.parameters[3];

		public byte Length => (byte)(this.parameters.Sum(p => p.Length) + 2);
		public InstructionDefinition Definition => InstructionDefinition.Find(this.Code);

		public byte SizeInBytes => Instruction.SizeToBytes(this.Size);
		public byte SizeInBits => Instruction.SizeToBits(this.Size);
		public ulong SizeMask => Instruction.SizeToMask(this.Size);

		public static byte SizeToBytes(InstructionSize size) => (byte)Math.Pow(2, (byte)size);
		public static byte SizeToBits(InstructionSize size) => (byte)(Instruction.SizeToBytes(size) * 8);
		public static ulong SizeToMask(InstructionSize size) => (1UL << (Instruction.SizeToBits(size) - 1)) | ((1UL << (Instruction.SizeToBits(size) - 1)) - 1);

		public Instruction(BinaryReader reader) {
			this.parameters = new List<Parameter>();

			this.Address = (ulong)reader.BaseStream.Position;

			var b1 = reader.ReadByte();
			var b2 = reader.ReadByte();

			this.Label = string.Empty;
			this.Code = (byte)((b1 & 0xFC) >> 2);
			this.Size = (InstructionSize)(b1 & 0x03);

			for (var i = 0; i < this.Definition.ParameterCount; i++) {
				switch ((ParameterType)((b2 >> (i * 2)) & 0x03)) {
					case ParameterType.RegisterAddress: this.parameters.Add(new Parameter(this.Size, ParameterType.RegisterAddress, reader.ReadByte())); break;
					case ParameterType.Register: this.parameters.Add(new Parameter(this.Size, ParameterType.Register, reader.ReadByte())); break;
					case ParameterType.LiteralAddress: this.parameters.Add(new Parameter(this.Size, ParameterType.LiteralAddress, reader.ReadUInt64())); break;
					case ParameterType.Literal:
						switch (this.Size) {
							case InstructionSize.OneByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadByte())); break;
							case InstructionSize.TwoByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt16())); break;
							case InstructionSize.FourByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt32())); break;
							case InstructionSize.EightByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt64())); break;
						}

						break;
				}
			}
		}

		public Instruction(string[] parts) : this(parts, string.Empty) {

		}

		public Instruction(string[] parts, string label) {
			this.Size = InstructionSize.EightByte;

			var index = parts[0].IndexOf(':');
			if (index != -1) {
				switch (parts[0][index + 1]) {
					case '1': this.Size = InstructionSize.OneByte; break;
					case '2': this.Size = InstructionSize.TwoByte; break;
					case '4': this.Size = InstructionSize.FourByte; break;
					case '8': this.Size = InstructionSize.EightByte; break;
				}

				parts[0] = parts[0].Substring(0, index);
			}

			this.Label = label;
			this.Address = 0;
			this.Code = InstructionDefinition.Find(parts[0]).Code;

			this.parameters = parts.Skip(1).Select(p => new Parameter(this.Size, p)).ToList();
		}

		public void Serialize(BinaryWriter writer, Dictionary<string, Instruction> labels) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));

			foreach (var p in this.parameters) {
				if (!string.IsNullOrWhiteSpace(p.Label)) {
					p.Literal = labels[p.Label].Address;
					p.Type = ParameterType.Literal;
				}
			}

			byte b = 0;
			for (var i = 0; i < this.Definition.ParameterCount; i++)
				b |= (byte)(((byte)this.parameters[i].Type) << (i * 2));

			writer.Write(b);

            this.parameters.ForEach(p => p.Serialize(writer));
		}

		public override string ToString() {
			return this.Definition.Mnemonic + ":" + this.SizeInBytes + " " + string.Join(" ", this.parameters.Select(p => p.ToString()));
		}
	}
}