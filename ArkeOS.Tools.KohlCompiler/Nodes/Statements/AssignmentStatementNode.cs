namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class AssignmentStatementNode : StatementNode {
        public LValueNode Target { get; }
        public ExpressionNode Expression { get; }

        public AssignmentStatementNode(LValueNode target, ExpressionNode expression) => (this.Target, this.Expression) = (target, expression);
    }
}
