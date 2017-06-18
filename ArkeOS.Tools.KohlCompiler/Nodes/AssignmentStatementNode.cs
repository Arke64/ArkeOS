namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class AssignmentStatementNode : StatementNode {
        public IdentifierNode Target { get; }
        public ExpressionNode Expression { get; }

        public AssignmentStatementNode(IdentifierNode identifier, ExpressionNode expression) => (this.Target, this.Expression) = (identifier, expression);
    }
}
