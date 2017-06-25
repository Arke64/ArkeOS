namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class WhileStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public WhileStatementNode(ExpressionStatementNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
