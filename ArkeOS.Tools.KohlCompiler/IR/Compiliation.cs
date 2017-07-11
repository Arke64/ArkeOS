using ArkeOS.Tools.KohlCompiler.Analysis;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariableSymbol> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariableSymbol> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public sealed class Function {
        public FunctionSymbol Symbol { get; }
        public BasicBlock Entry { get; }
        public IReadOnlyCollection<BasicBlock> AllBlocks { get; }

        public Function(FunctionSymbol symbol, BasicBlock entry, IReadOnlyCollection<BasicBlock> allBlocks) => (this.Symbol, this.Entry, this.AllBlocks) = (symbol, entry, allBlocks);

        public override string ToString() => $"func {this.Symbol}";
    }

    public sealed class BasicBlock {
        public ICollection<BasicBlockInstruction> Instructions { get; } = new List<BasicBlockInstruction>();
        public Terminator Terminator { get; set; }
    }
}
