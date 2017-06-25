namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CompoundAssignmentStatementNode : AssignmentStatementNode {
        public OperatorNode Op { get; }

        public CompoundAssignmentStatementNode(LValueNode target, OperatorNode op, ExpressionNode expression) : base(target, expression) => this.Op = op;
    }
}
