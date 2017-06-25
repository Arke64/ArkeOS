namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class AssignmentStatementNode : StatementNode {
        public LValueNode Target { get; }
        public ExpressionNode Expression { get; }

        public AssignmentStatementNode(LValueNode target, ExpressionNode expression) => (this.Target, this.Expression) = (target, expression);
    }
}
