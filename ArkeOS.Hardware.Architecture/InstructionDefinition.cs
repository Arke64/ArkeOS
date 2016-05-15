using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Hardware.Architecture {
	public class InstructionDefinition {
		private static Dictionary<string, InstructionDefinition> mnemonics;
		private static InstructionDefinition[] instructions;

		public string Mnemonic { get; }
		public byte Code { get; }
		public byte ParameterCount { get; }

		public ParameterDirection Parameter1Direction { get; }
		public ParameterDirection Parameter2Direction { get; }
		public ParameterDirection Parameter3Direction { get; }

		public static IReadOnlyList<InstructionDefinition> All => InstructionDefinition.instructions.Where(c => c != null).ToList();

		static InstructionDefinition() {
			InstructionDefinition.mnemonics = new Dictionary<string, InstructionDefinition>();
			InstructionDefinition.instructions = new InstructionDefinition[256];

			InstructionDefinition.Add("HLT", 0);
			InstructionDefinition.Add("NOP", 1);
			InstructionDefinition.Add("INT", 2, ParameterDirection.Read, ParameterDirection.Read);
			InstructionDefinition.Add("EINT", 3);
			InstructionDefinition.Add("INTE", 4);
			InstructionDefinition.Add("INTD", 5);
			InstructionDefinition.Add("XCHG", 6, ParameterDirection.Write, ParameterDirection.Write);
			InstructionDefinition.Add("CAS", 7, ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
			InstructionDefinition.Add("MOV", 8, ParameterDirection.Read, ParameterDirection.Write);

			InstructionDefinition.Add("ADD", 20, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("ADDF", 21, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("SUB", 22, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("SUBF", 23, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("DIV", 24, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("DIVF", 25, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("MUL", 26, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("MULF", 27, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("MOD", 28, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("MODF", 29, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);

			InstructionDefinition.Add("SR", 40, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("SL", 41, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("RR", 42, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("RL", 43, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("NAND", 44, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("AND", 45, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("NOR", 46, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("OR", 47, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("NXOR", 48, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("XOR", 49, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("NOT", 50, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("GT", 51, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("GTE", 52, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("LT", 53, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("LTE", 54, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("EQ", 55, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
			InstructionDefinition.Add("NEQ", 56, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);

			InstructionDefinition.Add("DBG", 60, ParameterDirection.Write);
			InstructionDefinition.Add("BRK", 61);
		}

		private static void Add(string mnemonic, byte code) => InstructionDefinition.Add(new InstructionDefinition(mnemonic, code));
		private static void Add(string mnemonic, byte code, ParameterDirection parameter1Direction) => InstructionDefinition.Add(new InstructionDefinition(mnemonic, code, parameter1Direction));
		private static void Add(string mnemonic, byte code, ParameterDirection parameter1Direction, ParameterDirection parameter2Direction) => InstructionDefinition.Add(new InstructionDefinition(mnemonic, code, parameter1Direction, parameter2Direction));
		private static void Add(string mnemonic, byte code, ParameterDirection parameter1Direction, ParameterDirection parameter2Direction, ParameterDirection parameter3Direction) => InstructionDefinition.Add(new InstructionDefinition(mnemonic, code, parameter1Direction, parameter2Direction, parameter3Direction));

		private InstructionDefinition(string mnemonic, byte code, params ParameterDirection[] directions) {
			this.Mnemonic = mnemonic;
			this.Code = code;
			this.ParameterCount = (byte)directions.Length;

			if (this.ParameterCount > 2) this.Parameter3Direction = directions[2];
			if (this.ParameterCount > 1) this.Parameter2Direction = directions[1];
			if (this.ParameterCount > 0) this.Parameter1Direction = directions[0];
		}

		private static void Add(InstructionDefinition def) {
			InstructionDefinition.mnemonics.Add(def.Mnemonic, def);
			InstructionDefinition.instructions[def.Code] = def;
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