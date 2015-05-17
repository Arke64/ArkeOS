using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.Executable {
	public class Instruction {
		public byte Code { get; }
		public InstructionSize Size { get; }
		public List<Parameter> All { get; }
		public Parameter A => this.All[0];
		public Parameter B => this.All[1];
		public Parameter C => this.All[2];
		public Parameter D => this.All[3];
		public byte Length => (byte)(this.All.Sum(p => p.Length) + 2);
		public InstructionDefinition Definition => InstructionDefinition.Find(this.Code);

		public Instruction(BinaryReader reader) {
			var b1 = reader.ReadByte();
			var b2 = reader.ReadByte();

			this.Code = (byte)((b1 & 0x3F) >> 2);
			this.Size = (InstructionSize)(b1 & 0x03);
			this.All = new List<Parameter>();

			for (var i = 0; i < this.Definition.ParameterCount; i++) {
				switch ((ParameterType)(b2 & (0x03 << (i * 2)))) {
					case ParameterType.Register: this.All.Add(new Parameter(this.Size, ParameterType.Register, reader.ReadByte())); break;
					case ParameterType.Literal: this.All.Add(new Parameter(this.Size, ParameterType.Literal, BitConverter.ToUInt64(reader.ReadBytes(2 ^ (byte)this.Size), 0))); break;
					case ParameterType.LiteralAddress: this.All.Add(new Parameter(this.Size, ParameterType.LiteralAddress, reader.ReadUInt64())); break;
					case ParameterType.RegisterAddress: this.All.Add(new Parameter(this.Size, ParameterType.RegisterAddress, reader.ReadByte())); break;
				}
			}
		}

		public Instruction(string[] parts) {
			this.Code = InstructionDefinition.Find(parts[0]).Code;
			this.Size = InstructionSize.EightByte;
			this.All = parts.Skip(1).Select(p => new Parameter(this.Size, p)).ToList();

			if (this.Definition.ParameterCount != this.All.Count()) throw new Exception();
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));

			byte b = 0;
			for (var i = 0; i < this.Definition.ParameterCount; i++)
				b |= (byte)(((byte)this.All[i].Type) << (i * 2));

			writer.Write(b);

            this.All.ForEach(p => p.Serialize(writer, this.Size));
		}
	}
}