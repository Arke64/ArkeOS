namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class WhileStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public WhileStatementNode(PositionInfo position, ExpressionStatementNode expression, StatementBlockNode statementBlock) : base(position) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
