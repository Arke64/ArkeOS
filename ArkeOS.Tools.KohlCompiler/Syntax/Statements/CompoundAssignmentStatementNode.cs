namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CompoundAssignmentStatementNode : AssignmentStatementNode {
        public OperatorNode Op { get; }

        public CompoundAssignmentStatementNode(PositionInfo position, ExpressionStatementNode target, OperatorNode op, ExpressionStatementNode expression) : base(position, target, expression) => this.Op = op;
    }
}
