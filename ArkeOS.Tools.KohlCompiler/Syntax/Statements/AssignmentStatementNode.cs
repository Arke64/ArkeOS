namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class AssignmentStatementNode : StatementNode {
        public ExpressionStatementNode Target { get; }
        public ExpressionStatementNode Expression { get; }

        public AssignmentStatementNode(ExpressionStatementNode target, ExpressionStatementNode expression) => (this.Target, this.Expression) = (target, expression);
    }
}
