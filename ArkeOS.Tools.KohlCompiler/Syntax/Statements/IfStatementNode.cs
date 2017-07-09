namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class IfStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public IfStatementNode(PositionInfo position, ExpressionStatementNode expression, StatementBlockNode statementBlock) : base(position) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
