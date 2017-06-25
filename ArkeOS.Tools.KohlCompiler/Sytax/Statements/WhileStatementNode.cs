namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class WhileStatementNode : BlockStatementNode {
        public ExpressionNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public WhileStatementNode(ExpressionNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
