using System.Collections.Generic;

namespace ArkeOS.Executable {
	public class InstructionDefinition {
		private static Dictionary<string, InstructionDefinition> mnemonics = new Dictionary<string, InstructionDefinition>();
		private static Dictionary<byte, InstructionDefinition> codes = new Dictionary<byte, InstructionDefinition>();
		private static List<InstructionDefinition> list = new List<InstructionDefinition>();

		public string Mnemonic { get; }
		public byte Code { get; }
		public byte ParameterCount { get; }

		public static IReadOnlyList<InstructionDefinition> All => InstructionDefinition.list;

		private InstructionDefinition(string mnemonic, byte code, byte parameterCount) {
			this.Mnemonic = mnemonic;
			this.Code = code;
			this.ParameterCount = parameterCount;

			InstructionDefinition.mnemonics.Add(mnemonic, this);
			InstructionDefinition.codes.Add(code, this);
			InstructionDefinition.list.Add(this);
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
		public static InstructionDefinition Int = new InstructionDefinition("INT", 2, 1);
		public static InstructionDefinition Eint = new InstructionDefinition("EINT", 3, 1);
		public static InstructionDefinition Mov = new InstructionDefinition("MOV", 4, 2);
		public static InstructionDefinition Xchg = new InstructionDefinition("XCHG", 5, 2);
		public static InstructionDefinition In = new InstructionDefinition("IN", 6, 2);
		public static InstructionDefinition Out = new InstructionDefinition("OUT", 7, 2);
		public static InstructionDefinition Push = new InstructionDefinition("PUSH", 8, 1);
		public static InstructionDefinition Pop = new InstructionDefinition("POP", 9, 1);
		public static InstructionDefinition Jz = new InstructionDefinition("JZ", 10, 2);
		public static InstructionDefinition Jnz = new InstructionDefinition("JNZ", 11, 2);
		public static InstructionDefinition Jiz = new InstructionDefinition("JMP", 12, 1);

		public static InstructionDefinition Add = new InstructionDefinition("ADD", 20, 3);
		public static InstructionDefinition Adc = new InstructionDefinition("ADC", 21, 3);
		public static InstructionDefinition Adf = new InstructionDefinition("ADF", 22, 3);
		public static InstructionDefinition Sub = new InstructionDefinition("SUB", 23, 3);
		public static InstructionDefinition Sbb = new InstructionDefinition("SBB", 24, 3);
		public static InstructionDefinition Sbf = new InstructionDefinition("SBF", 25, 3);
		public static InstructionDefinition Div = new InstructionDefinition("DIV", 26, 3);
		public static InstructionDefinition Dvf = new InstructionDefinition("DVF", 27, 3);
		public static InstructionDefinition Mul = new InstructionDefinition("MUL", 28, 3);
		public static InstructionDefinition Mlf = new InstructionDefinition("MLF", 39, 3);
		public static InstructionDefinition Inc = new InstructionDefinition("INC", 30, 2);
		public static InstructionDefinition Dec = new InstructionDefinition("DEC", 31, 2);
		public static InstructionDefinition Neg = new InstructionDefinition("NEG", 32, 2);
		public static InstructionDefinition Mod = new InstructionDefinition("MOD", 33, 3);
		public static InstructionDefinition Mdf = new InstructionDefinition("MDF", 34, 3);

		public static InstructionDefinition Rr = new InstructionDefinition("SR", 40, 3);
		public static InstructionDefinition Rl = new InstructionDefinition("SL", 41, 3);
		public static InstructionDefinition Rrc = new InstructionDefinition("RR", 42, 3);
		public static InstructionDefinition Rlc = new InstructionDefinition("RL", 43, 3);
		public static InstructionDefinition Nand = new InstructionDefinition("NAND", 44, 3);
		public static InstructionDefinition And = new InstructionDefinition("AND", 45, 3);
		public static InstructionDefinition Nor = new InstructionDefinition("NOR", 46, 3);
		public static InstructionDefinition Or = new InstructionDefinition("OR", 47, 3);
		public static InstructionDefinition Nxor = new InstructionDefinition("NXOR", 48, 3);
		public static InstructionDefinition Xor = new InstructionDefinition("XOR", 49, 3);
		public static InstructionDefinition Not = new InstructionDefinition("NOT", 50, 2);
		public static InstructionDefinition Gt = new InstructionDefinition("GT", 51, 2);
		public static InstructionDefinition Gte = new InstructionDefinition("GTE", 52, 2);
		public static InstructionDefinition Lt = new InstructionDefinition("LT", 53, 2);
		public static InstructionDefinition Lte = new InstructionDefinition("LTE", 54, 2);
		public static InstructionDefinition Eq = new InstructionDefinition("EQ", 55, 2);
		public static InstructionDefinition Neq = new InstructionDefinition("NEQ", 56, 2);
	}
}