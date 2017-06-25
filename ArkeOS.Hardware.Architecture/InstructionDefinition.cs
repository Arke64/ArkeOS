using System.Collections.Generic;

namespace ArkeOS.Hardware.Architecture {
    public class InstructionDefinition {
        private static readonly Dictionary<string, InstructionDefinition> mnemonics = new Dictionary<string, InstructionDefinition>();
        private static readonly InstructionDefinition[] instructions = new InstructionDefinition[256];

        public static InstructionDefinition HLT { get; } = new InstructionDefinition(nameof(InstructionDefinition.HLT), 0);
        public static InstructionDefinition NOP { get; } = new InstructionDefinition(nameof(InstructionDefinition.NOP), 1);
        public static InstructionDefinition INT { get; } = new InstructionDefinition(nameof(InstructionDefinition.INT), 2, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition EINT { get; } = new InstructionDefinition(nameof(InstructionDefinition.EINT), 3);
        public static InstructionDefinition INTE { get; } = new InstructionDefinition(nameof(InstructionDefinition.INTE), 4);
        public static InstructionDefinition INTD { get; } = new InstructionDefinition(nameof(InstructionDefinition.INTD), 5);
        public static InstructionDefinition XCHG { get; } = new InstructionDefinition(nameof(InstructionDefinition.XCHG), 6, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
        public static InstructionDefinition CAS { get; } = new InstructionDefinition(nameof(InstructionDefinition.CAS), 7, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition SET { get; } = new InstructionDefinition(nameof(InstructionDefinition.SET), 8, ParameterDirection.Write, ParameterDirection.Read);
        public static InstructionDefinition CPY { get; } = new InstructionDefinition(nameof(InstructionDefinition.CPY), 9, ParameterDirection.Read, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition CALL { get; } = new InstructionDefinition(nameof(InstructionDefinition.CALL), 10, ParameterDirection.Read);
        public static InstructionDefinition RET { get; } = new InstructionDefinition(nameof(InstructionDefinition.RET), 11);

        public static InstructionDefinition ADD { get; } = new InstructionDefinition(nameof(InstructionDefinition.ADD), 20, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition ADDF { get; } = new InstructionDefinition(nameof(InstructionDefinition.ADDF), 21, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition SUB { get; } = new InstructionDefinition(nameof(InstructionDefinition.SUB), 22, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition SUBF { get; } = new InstructionDefinition(nameof(InstructionDefinition.SUBF), 23, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition DIV { get; } = new InstructionDefinition(nameof(InstructionDefinition.DIV), 24, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition DIVF { get; } = new InstructionDefinition(nameof(InstructionDefinition.DIVF), 25, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition MUL { get; } = new InstructionDefinition(nameof(InstructionDefinition.MUL), 26, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition MULF { get; } = new InstructionDefinition(nameof(InstructionDefinition.MULF), 27, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition POW { get; } = new InstructionDefinition(nameof(InstructionDefinition.POW), 28, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition POWF { get; } = new InstructionDefinition(nameof(InstructionDefinition.POWF), 29, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition MOD { get; } = new InstructionDefinition(nameof(InstructionDefinition.MOD), 30, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition MODF { get; } = new InstructionDefinition(nameof(InstructionDefinition.MODF), 31, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition ITOF { get; } = new InstructionDefinition(nameof(InstructionDefinition.ITOF), 32, ParameterDirection.Write, ParameterDirection.Read);
        public static InstructionDefinition FTOI { get; } = new InstructionDefinition(nameof(InstructionDefinition.FTOI), 33, ParameterDirection.Write, ParameterDirection.Read);

        public static InstructionDefinition SR { get; } = new InstructionDefinition(nameof(InstructionDefinition.SR), 40, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition SL { get; } = new InstructionDefinition(nameof(InstructionDefinition.SL), 41, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition RR { get; } = new InstructionDefinition(nameof(InstructionDefinition.RR), 42, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition RL { get; } = new InstructionDefinition(nameof(InstructionDefinition.RL), 43, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition NAND { get; } = new InstructionDefinition(nameof(InstructionDefinition.NAND), 44, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition AND { get; } = new InstructionDefinition(nameof(InstructionDefinition.AND), 45, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition NOR { get; } = new InstructionDefinition(nameof(InstructionDefinition.NOR), 46, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition OR { get; } = new InstructionDefinition(nameof(InstructionDefinition.OR), 47, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition NXOR { get; } = new InstructionDefinition(nameof(InstructionDefinition.NXOR), 48, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition XOR { get; } = new InstructionDefinition(nameof(InstructionDefinition.XOR), 49, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition NOT { get; } = new InstructionDefinition(nameof(InstructionDefinition.NOT), 50, ParameterDirection.Write, ParameterDirection.Read);
        public static InstructionDefinition GT { get; } = new InstructionDefinition(nameof(InstructionDefinition.GT), 51, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition GTE { get; } = new InstructionDefinition(nameof(InstructionDefinition.GTE), 52, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition LT { get; } = new InstructionDefinition(nameof(InstructionDefinition.LT), 53, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition LTE { get; } = new InstructionDefinition(nameof(InstructionDefinition.LTE), 54, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition EQ { get; } = new InstructionDefinition(nameof(InstructionDefinition.EQ), 55, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);
        public static InstructionDefinition NEQ { get; } = new InstructionDefinition(nameof(InstructionDefinition.NEQ), 56, ParameterDirection.Write, ParameterDirection.Read, ParameterDirection.Read);

        public static InstructionDefinition DBG { get; } = new InstructionDefinition(nameof(InstructionDefinition.DBG), 60, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read, ParameterDirection.Write | ParameterDirection.Read);
        public static InstructionDefinition BRK { get; } = new InstructionDefinition(nameof(InstructionDefinition.BRK), 61);

        public string Mnemonic { get; }
        public byte Code { get; }
        public byte ParameterCount { get; }

        public ParameterDirection Parameter1Direction { get; }
        public ParameterDirection Parameter2Direction { get; }
        public ParameterDirection Parameter3Direction { get; }

        private InstructionDefinition(string mnemonic, byte code, params ParameterDirection[] directions) {
            this.Mnemonic = mnemonic;
            this.Code = code;
            this.ParameterCount = (byte)directions.Length;

            if (this.ParameterCount > 2) this.Parameter3Direction = directions[2];
            if (this.ParameterCount > 1) this.Parameter2Direction = directions[1];
            if (this.ParameterCount > 0) this.Parameter1Direction = directions[0];

            InstructionDefinition.mnemonics.Add(this.Mnemonic, this);
            InstructionDefinition.instructions[this.Code] = this;
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
