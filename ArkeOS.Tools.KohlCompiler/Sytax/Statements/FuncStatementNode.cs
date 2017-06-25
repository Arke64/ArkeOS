namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FuncStatementNode : BlockStatementNode {
        public IdentifierNode Identifier { get; }
        public StatementBlockNode StatementBlock { get; }

        public FuncStatementNode(IdentifierNode identifier, StatementBlockNode statementBlock) => (this.Identifier, this.StatementBlock) = (identifier, statementBlock);
    }
}
