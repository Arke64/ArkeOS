namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ProgramNode : SyntaxNode {
        public StatementBlockNode StatementBlock { get; }

        public ProgramNode(StatementBlockNode statementBlock) => this.StatementBlock = statementBlock;
    }
}
