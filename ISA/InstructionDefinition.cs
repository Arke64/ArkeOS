using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.ISA {
	public class InstructionDefinition {
		private static Dictionary<string, InstructionDefinition> mnemonics;
		private static InstructionDefinition[] instructions;

		public string Mnemonic { get; }
		public byte Code { get; }
		public byte ParameterCount { get; }

		public static IReadOnlyList<InstructionDefinition> All => InstructionDefinition.instructions.Where(c => c != null).ToList();

		static InstructionDefinition() {
			InstructionDefinition.mnemonics = new Dictionary<string, InstructionDefinition>();
			InstructionDefinition.instructions = new InstructionDefinition[256];

			new InstructionDefinition("HLT", 0, 0);
			new InstructionDefinition("NOP", 1, 0);
			new InstructionDefinition("INT", 2, 1);
			new InstructionDefinition("EINT", 3, 0);
			new InstructionDefinition("MOV", 4, 2);
			new InstructionDefinition("XCHG", 5, 2);
			new InstructionDefinition("IN", 6, 2);
			new InstructionDefinition("OUT", 7, 2);
			new InstructionDefinition("PUSH", 8, 1);
			new InstructionDefinition("POP", 9, 1);
			new InstructionDefinition("JZ", 10, 2);
			new InstructionDefinition("JNZ", 11, 2);
			new InstructionDefinition("JMP", 12, 1);

			new InstructionDefinition("ADD", 20, 3);
			new InstructionDefinition("ADC", 21, 3);
			new InstructionDefinition("ADF", 22, 3);
			new InstructionDefinition("SUB", 23, 3);
			new InstructionDefinition("SBB", 24, 3);
			new InstructionDefinition("SBF", 25, 3);
			new InstructionDefinition("DIV", 26, 3);
			new InstructionDefinition("DVF", 27, 3);
			new InstructionDefinition("MUL", 28, 3);
			new InstructionDefinition("MLF", 39, 3);
			new InstructionDefinition("INC", 30, 2);
			new InstructionDefinition("DEC", 31, 2);
			new InstructionDefinition("NEG", 32, 2);
			new InstructionDefinition("MOD", 33, 3);
			new InstructionDefinition("MDF", 34, 3);

			new InstructionDefinition("SR", 40, 3);
			new InstructionDefinition("SL", 41, 3);
			new InstructionDefinition("RR", 42, 3);
			new InstructionDefinition("RL", 43, 3);
			new InstructionDefinition("NAND", 44, 3);
			new InstructionDefinition("AND", 45, 3);
			new InstructionDefinition("NOR", 46, 3);
			new InstructionDefinition("OR", 47, 3);
			new InstructionDefinition("NXOR", 48, 3);
			new InstructionDefinition("XOR", 49, 3);
			new InstructionDefinition("NOT", 50, 2);
			new InstructionDefinition("GT", 51, 2);
			new InstructionDefinition("GTE", 52, 2);
			new InstructionDefinition("LT", 53, 2);
			new InstructionDefinition("LTE", 54, 2);
			new InstructionDefinition("EQ", 55, 2);
			new InstructionDefinition("NEQ", 56, 2);
		}

		private InstructionDefinition(string mnemonic, byte code, byte parameterCount) {
			this.Mnemonic = mnemonic;
			this.Code = code;
			this.ParameterCount = parameterCount;

			InstructionDefinition.mnemonics.Add(mnemonic, this);
			InstructionDefinition.instructions[code] = this;
		}

		public static InstructionDefinition Find(string mnemonic) {
			if (!InstructionDefinition.mnemonics.ContainsKey(mnemonic))
				return null;

			return InstructionDefinition.mnemonics[mnemonic];
		}

		public static InstructionDefinition Find(byte code) {
			return InstructionDefinition.instructions[code];
		}
	}
}