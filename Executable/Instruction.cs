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

			this.Code = (byte)((b1 & 0xFC) >> 2);
			this.Size = (InstructionSize)(b1 & 0x03);
			this.All = new List<Parameter>();

			for (var i = 0; i < this.Definition.ParameterCount; i++) {
				switch ((ParameterType)((b2 >> (i * 2)) & 0x03)) {
					case ParameterType.Register: this.All.Add(new Parameter(this.Size, ParameterType.Register, reader.ReadByte())); break;
					case ParameterType.LiteralAddress: this.All.Add(new Parameter(this.Size, ParameterType.LiteralAddress, reader.ReadUInt64())); break;
					case ParameterType.RegisterAddress: this.All.Add(new Parameter(this.Size, ParameterType.RegisterAddress, reader.ReadByte())); break;
					case ParameterType.Literal:
						switch (this.Size) {
							case InstructionSize.OneByte: this.All.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadByte())); break;
							case InstructionSize.TwoByte: this.All.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt16())); break;
							case InstructionSize.FourByte: this.All.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt32())); break;
							case InstructionSize.EightByte: this.All.Add(new Parameter(this.Size, ParameterType.Literal, reader.ReadUInt64())); break;
						}

						break;
				}
			}
		}

		public Instruction(string[] parts) {
			this.Size = InstructionSize.EightByte;

			var idx = parts[0].IndexOf(':');
            if (idx != -1) {
				this.Size = (InstructionSize)(byte.Parse(parts[0].Substring(idx + 1)) - 1);
				parts[0] = parts[0].Substring(0, idx);
			}

			this.Code = InstructionDefinition.Find(parts[0]).Code;
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