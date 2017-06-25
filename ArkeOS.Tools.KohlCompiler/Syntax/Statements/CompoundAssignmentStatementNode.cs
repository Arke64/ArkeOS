namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CompoundAssignmentStatementNode : AssignmentStatementNode {
        public OperatorNode Op { get; }

        public CompoundAssignmentStatementNode(ExpressionStatementNode target, OperatorNode op, ExpressionStatementNode expression) : base(target, expression) => this.Op = op;
    }
}
