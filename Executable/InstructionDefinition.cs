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
		public static InstructionDefinition Mov = new InstructionDefinition("MOV", 3, 2);
		public static InstructionDefinition In = new InstructionDefinition("IN", 4, 2);
		public static InstructionDefinition Out = new InstructionDefinition("OUT", 5, 2);
		public static InstructionDefinition Push = new InstructionDefinition("PUSH", 6, 1);
		public static InstructionDefinition Pop = new InstructionDefinition("POP", 7, 1);
		public static InstructionDefinition Jz = new InstructionDefinition("JZ", 8, 2);
		public static InstructionDefinition Jnz = new InstructionDefinition("JNZ", 9, 2);
		public static InstructionDefinition Jiz = new InstructionDefinition("JMP", 10, 1);

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
		public static InstructionDefinition Xchg = new InstructionDefinition("XCHG", 44, 2);
		public static InstructionDefinition Nand = new InstructionDefinition("NAND", 45, 3);
		public static InstructionDefinition And = new InstructionDefinition("AND", 46, 3);
		public static InstructionDefinition Nor = new InstructionDefinition("NOR", 47, 3);
		public static InstructionDefinition Or = new InstructionDefinition("OR", 48, 3);
		public static InstructionDefinition Nxor = new InstructionDefinition("NXOR", 49, 3);
		public static InstructionDefinition Xor = new InstructionDefinition("XOR", 50, 3);
		public static InstructionDefinition Not = new InstructionDefinition("NOT", 51, 2);
		public static InstructionDefinition Gt = new InstructionDefinition("GT", 52, 2);
		public static InstructionDefinition Gte = new InstructionDefinition("GTE", 53, 2);
		public static InstructionDefinition Lt = new InstructionDefinition("LT", 54, 2);
		public static InstructionDefinition Lte = new InstructionDefinition("LTE", 55, 2);
		public static InstructionDefinition Eq = new InstructionDefinition("EQ", 56, 2);
		public static InstructionDefinition Neq = new InstructionDefinition("NEQ", 57, 2);
	}
}