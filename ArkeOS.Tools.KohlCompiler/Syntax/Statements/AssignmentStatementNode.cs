namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class AssignmentStatementNode : StatementNode {
        public ExpressionStatementNode Target { get; }
        public ExpressionStatementNode Expression { get; }

        public AssignmentStatementNode(PositionInfo position, ExpressionStatementNode target, ExpressionStatementNode expression) : base(position) => (this.Target, this.Expression) = (target, expression);
    }
}
