using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Architecture {
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

        [Flags]
        public enum ParameterDirection {
            Read = 1,
            Write = 2,
        }

        static InstructionDefinition() {
            InstructionDefinition.mnemonics = new Dictionary<string, InstructionDefinition>();
            InstructionDefinition.instructions = new InstructionDefinition[256];

            new InstructionDefinition("HLT", 0);
            new InstructionDefinition("NOP", 1);
            new InstructionDefinition("INT", 2, ParameterDirection.Read, ParameterDirection.Read);
            new InstructionDefinition("EINT", 3);
            new InstructionDefinition("INTE", 4);
            new InstructionDefinition("INTD", 5);
            new InstructionDefinition("XCHG", 6, ParameterDirection.Write, ParameterDirection.Write);
            new InstructionDefinition("CAS", 7, ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
            new InstructionDefinition("MOV", 8, ParameterDirection.Read, ParameterDirection.Write);

            new InstructionDefinition("ADD", 20, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("ADDF", 21, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("SUB", 22, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("SUBF", 23, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("DIV", 24, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("DIVF", 25, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("MUL", 26, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("MULF", 27, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("MOD", 28, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("MODF", 29, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);

            new InstructionDefinition("SR", 40, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("SL", 41, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("RR", 42, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("RL", 43, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("NAND", 44, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("AND", 45, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("NOR", 46, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("OR", 47, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("NXOR", 48, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("XOR", 49, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("NOT", 50, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("GT", 51, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("GTE", 52, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("LT", 53, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("LTE", 54, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("EQ", 55, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);
            new InstructionDefinition("NEQ", 56, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Write);

            new InstructionDefinition("DBG", 60, ParameterDirection.Write);
            new InstructionDefinition("BRK", 61);
        }

        private InstructionDefinition(string mnemonic, byte code) : this(mnemonic, code, (byte)0) {

        }

        private InstructionDefinition(string mnemonic, byte code, ParameterDirection parameter1Direction) : this(mnemonic, code, 1) {
            this.Parameter1Direction = parameter1Direction;
        }

        private InstructionDefinition(string mnemonic, byte code, ParameterDirection parameter1Direction, ParameterDirection parameter2Direction) : this(mnemonic, code, 2) {
            this.Parameter1Direction = parameter1Direction;
            this.Parameter2Direction = parameter2Direction;
        }

        private InstructionDefinition(string mnemonic, byte code, ParameterDirection parameter1Direction, ParameterDirection parameter2Direction, ParameterDirection parameter3Direction) : this(mnemonic, code, 3) {
            this.Parameter1Direction = parameter1Direction;
            this.Parameter2Direction = parameter2Direction;
            this.Parameter3Direction = parameter3Direction;
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