namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IfStatementNode : StatementNode {
        public ExpressionNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public IfStatementNode(ExpressionNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
