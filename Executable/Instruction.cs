using System;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Executable {
	public class Instruction {
		private static Dictionary<string, Instruction> mnemonics = new Dictionary<string, Instruction>();
		private static Dictionary<byte, Instruction> codes = new Dictionary<byte, Instruction>();

		public string Mnemonic { get; }
		public byte Code { get; }
		public bool SupportsRa { get; }
		public bool SupportsRb { get; }
		public bool SupportsRc { get; }
		public bool SupportsValue { get; }

		public InstructionSize Size { get; set; }
		public Register Ra { get; set; }
		public Register Rb { get; set; }
		public Register Rc { get; set; }
		public ulong Value { get; set; }

		public byte Length => (byte)(1 + (this.SupportsRa ? 1 : 0) + (this.SupportsRb ? 1 : 0) + (this.SupportsRc ? 1 : 0) + (this.SupportsValue ? 2 ^ (int)this.Size : 0));

		private Instruction(string mnemonic, byte code, bool supportsRa, bool supportsRb, bool supportsRc, bool supportsValue) {
			this.Mnemonic = mnemonic;
			this.Code = code;
			this.SupportsRa = supportsRa;
			this.SupportsRb = supportsRb;
			this.SupportsRc = supportsRc;
			this.SupportsValue = supportsValue;

			this.Size = InstructionSize.OneByte;
			this.Ra = (Register)0xFF;
			this.Rb = (Register)0xFF;
			this.Rc = (Register)0xFF;

			Instruction.mnemonics.Add(mnemonic, this);
			Instruction.codes.Add(code, this);
		}

		public Instruction(BinaryReader reader) {
			var b1 = reader.ReadByte();

			this.Code = (byte)((b1 & 0x3F) >> 2);
			this.Size = (InstructionSize)(b1 & 0x03);

			var def = Instruction.Find(this.Code);

			this.Mnemonic = def.Mnemonic;

			if (def.SupportsRa) {
				var b2 = reader.ReadByte();

				this.SupportsRa = true;
				this.Ra = (Register)(b2 & 0x7F);

				if (def.SupportsRb && (b2 & 0x80) == 0) {
					var b3 = reader.ReadByte();

					this.SupportsRb = true;
					this.Rb = (Register)(b3 & 0x7F);

					if (def.SupportsRc && (b3 & 0x80) == 0) {
						var b4 = reader.ReadByte();

						this.SupportsRc = true;
						this.Rc = (Register)(b4 & 0x7F);
					}
				}
			}

			if (def.SupportsValue) {
				this.SupportsValue = true;

				switch (this.Size) {
					case InstructionSize.OneByte: this.Value = reader.ReadByte(); break;
					case InstructionSize.TwoByte: this.Value = reader.ReadUInt16(); break;
					case InstructionSize.FourByte: this.Value = reader.ReadUInt32(); break;
					case InstructionSize.EightByte: this.Value = reader.ReadUInt64(); break;
				}
			}
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write((byte)((this.Code << 2) | (((byte)this.Size) & 0x03)));

			if (this.SupportsRa)
				writer.Write((byte)((this.SupportsRb && Enum.IsDefined(typeof(Register), this.Rb) ? 0x00 : 0x80) | (byte)this.Ra));

			if (this.SupportsRb)
				writer.Write((byte)((this.SupportsRc && Enum.IsDefined(typeof(Register), this.Rc) ? 0x00 : 0x80) | (byte)this.Rb));

			if (this.SupportsRc)
				writer.Write((byte)((this.SupportsValue ? 0x00 : 0x80) | (byte)this.Rc));

			if (this.SupportsValue) {
				switch (this.Size) {
					case InstructionSize.OneByte: writer.Write((byte)this.Value); break;
					case InstructionSize.TwoByte: writer.Write((ushort)this.Value); break;
					case InstructionSize.FourByte: writer.Write((uint)this.Value); break;
					case InstructionSize.EightByte: writer.Write(this.Value); break;
				}
			}
		}

		public static Instruction Find(string mnemonic) {
			if (!Instruction.mnemonics.ContainsKey(mnemonic))
				return null;

			return Instruction.mnemonics[mnemonic];
		}

		public static Instruction Find(byte code) {
			if (!Instruction.codes.ContainsKey(code))
				return null;

			return Instruction.codes[code];
		}

		public static Instruction Hlt = new Instruction("HLT", 0, false, false, false, false);
		public static Instruction Nop = new Instruction("NOP", 1, false, false, false, false);
		public static Instruction Add = new Instruction("ADD", 5, true, true, true, false);
		public static Instruction Jiz = new Instruction("JIZ", 51, false, false, false, true);
	}
}