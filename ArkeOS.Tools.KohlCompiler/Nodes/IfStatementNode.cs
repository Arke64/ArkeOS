namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IfStatementNode : StatementNode {
        public ExpressionNode Expression { get; }
        public StatementNode Statement { get; }

        public IfStatementNode(ExpressionNode expression, StatementNode statement) => (this.Expression, this.Statement) = (expression, statement);
    }
}
