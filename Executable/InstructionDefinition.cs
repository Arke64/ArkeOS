using System;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Executable {
	public class InstructionDefinition {
		private static Dictionary<string, InstructionDefinition> mnemonics = new Dictionary<string, InstructionDefinition>();
		private static Dictionary<byte, InstructionDefinition> codes = new Dictionary<byte, InstructionDefinition>();

		public string Mnemonic { get; }
		public byte Code { get; }
		public byte ParameterCount { get; }

		private InstructionDefinition(string mnemonic, byte code, byte parameterCount) {
			this.Mnemonic = mnemonic;
			this.Code = code;
			this.ParameterCount = parameterCount;

			InstructionDefinition.mnemonics.Add(mnemonic, this);
			InstructionDefinition.codes.Add(code, this);
		}

		public static InstructionDefinition Find(string mnemonic) {
			if (!InstructionDefinition.mnemonics.ContainsKey(mnemonic))
				return null;

			return InstructionDefinition.mnemonics[mnemonic];
		}

		public static InstructionDefinition Find(byte code) {
			if (!InstructionDefinition.codes.ContainsKey(code))
				return null;

			return InstructionDefinition.codes[code];
		}

		public static InstructionDefinition Hlt = new InstructionDefinition("HLT", 0, 0);
		public static InstructionDefinition Nop = new InstructionDefinition("NOP", 1, 0);
		public static InstructionDefinition Add = new InstructionDefinition("ADD", 5, 3);
		public static InstructionDefinition Jiz = new InstructionDefinition("JZ", 8, 2);
	}
}