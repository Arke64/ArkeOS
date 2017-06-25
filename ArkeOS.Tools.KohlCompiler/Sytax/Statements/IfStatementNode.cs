namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IfStatementNode : BlockStatementNode {
        public ExpressionNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public IfStatementNode(ExpressionNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
