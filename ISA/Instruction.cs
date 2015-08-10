using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.ISA {
	public class Instruction {
		private List<Parameter> parameters;

		public ulong Address { get; set; }
		public string Label { get; set; }
		public byte Code { get; }
		public InstructionSize Size { get; }
		public byte Length { get; }
		public InstructionDefinition Definition { get; }

		public Parameter Parameter1 => this.parameters[0];
		public Parameter Parameter2 => this.parameters[1];
		public Parameter Parameter3 => this.parameters[2];
		public Parameter Parameter4 => this.parameters[3];

		public byte SizeInBytes => Instruction.SizeToBytes(this.Size);
		public byte SizeInBits => Instruction.SizeToBits(this.Size);
		public ulong SizeMask => Instruction.SizeToMask(this.Size);

		public static byte SizeToBytes(InstructionSize size) => (byte)(1 << (byte)size);
		public static byte SizeToBits(InstructionSize size) => (byte)(Instruction.SizeToBytes(size) * 8);
		public static ulong SizeToMask(InstructionSize size) => (1UL << (Instruction.SizeToBits(size) - 1)) | ((1UL << (Instruction.SizeToBits(size) - 1)) - 1);

		public Instruction(byte[] memory, ulong address) {
			this.parameters = new List<Parameter>();

			this.Address = address;

			var b1 = memory[address++];
			var b2 = memory[address++];

			this.Size = (InstructionSize)(b1 & 0x03);
			this.Label = string.Empty;
			this.Code = (byte)((b1 & 0xFC) >> 2);
			this.Definition = InstructionDefinition.Find(this.Code);
			this.Length = 2;

			for (var i = 0; i < this.Definition.ParameterCount; i++) {
				switch ((ParameterType)((b2 >> (i * 2)) & 0x03)) {
					case ParameterType.RegisterAddress: this.parameters.Add(new Parameter(this.Size, ParameterType.RegisterAddress, memory[address++])); break;
					case ParameterType.Register: this.parameters.Add(new Parameter(this.Size, ParameterType.Register, memory[address++])); break;
					case ParameterType.LiteralAddress: this.parameters.Add(new Parameter(this.Size, ParameterType.LiteralAddress, BitConverter.ToUInt64(memory, (int)address))); address += 8; break;
					case ParameterType.Literal:
						switch (this.Size) {
							case InstructionSize.OneByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, memory[address++])); break;
							case InstructionSize.TwoByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, BitConverter.ToUInt16(memory, (int)address))); address += 2; break;
							case InstructionSize.FourByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, BitConverter.ToUInt32(memory, (int)address))); address += 4; break;
							case InstructionSize.EightByte: this.parameters.Add(new Parameter(this.Size, ParameterType.Literal, BitConverter.ToUInt64(memory, (int)address))); address += 8; break;
						}

						break;
				}

				this.Length += this.parameters[i].Length;
			}
		}

		public Instruction(string[] parts) {
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

			this.Label = string.Empty;
			this.Address = 0;
			this.Code = InstructionDefinition.Find(parts[0]).Code;
			this.Definition = InstructionDefinition.Find(this.Code);

			this.parameters = parts.Skip(1).Select(p => new Parameter(this.Size, p)).ToList();

			this.Length = (byte)(this.parameters.Sum(p => p.Length) + 2);
		}

		public void Serialize(BinaryWriter writer, Dictionary<string, ulong> labels) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));

			foreach (var p in this.parameters) {
				if (!string.IsNullOrWhiteSpace(p.Label)) {
					p.Literal = labels[p.Label];
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