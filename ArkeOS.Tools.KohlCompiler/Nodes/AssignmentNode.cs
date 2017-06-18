namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class AssignmentNode : StatementNode {
        public IdentifierNode Target { get; }
        public ExpressionNode Expression { get; }

        public AssignmentNode(IdentifierNode identifier, ExpressionNode expression) => (this.Target, this.Expression) = (identifier, expression);
    }
}
