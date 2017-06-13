using System.Collections.Generic;

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

        static InstructionDefinition() {
            InstructionDefinition.mnemonics = new Dictionary<string, InstructionDefinition>();
            InstructionDefinition.instructions = new InstructionDefinition[256];

            InstructionDefinition.Add("HLT", 0);
            InstructionDefinition.Add("NOP", 1);
            InstructionDefinition.Add("INT", 2, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("EINT", 3);
            InstructionDefinition.Add("INTE", 4);
            InstructionDefinition.Add("INTD", 5);
            InstructionDefinition.Add("XCHG", 6, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
            InstructionDefinition.Add("CAS", 7, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("SET", 8, ParameterDirection.Write, ParameterDirection.Read);
            InstructionDefinition.Add("CPY", 9, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("CALL", 10, ParameterDirection.Read);
            InstructionDefinition.Add("RET", 11);

            InstructionDefinition.Add("ADD", 20, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("ADDF", 21, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("SUB", 22, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("SUBF", 23, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("DIV", 24, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("DIVF", 25, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("MUL", 26, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("MULF", 27, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("POW", 28, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("POWF", 29, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("MOD", 30, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("MODF", 31, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("ITOF", 32, ParameterDirection.Write, ParameterDirection.Read);
            InstructionDefinition.Add("FTOI", 33, ParameterDirection.Write, ParameterDirection.Read);

            InstructionDefinition.Add("SR", 40, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("SL", 41, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("RR", 42, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("RL", 43, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("NAND", 44, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("AND", 45, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("NOR", 46, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("OR", 47, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("NXOR", 48, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("XOR", 49, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("NOT", 50, ParameterDirection.Write, ParameterDirection.Read);
            InstructionDefinition.Add("GT", 51, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("GTE", 52, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("LT", 53, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("LTE", 54, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("EQ", 55, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
            InstructionDefinition.Add("NEQ", 56, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);

            InstructionDefinition.Add("DBG", 60, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
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

        public static InstructionDefinition Find(byte code) => InstructionDefinition.instructions[code];
        public static bool IsCodeValid(byte code) => InstructionDefinition.instructions[code] != null;
    }
}
