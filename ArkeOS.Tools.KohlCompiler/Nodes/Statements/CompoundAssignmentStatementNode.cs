namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class CompoundAssignmentStatementNode : AssignmentStatementNode {
        public OperatorNode Op { get; }

        public CompoundAssignmentStatementNode(LValueNode target, OperatorNode op, ExpressionNode expression) : base(target, expression) => this.Op = op;
    }
}
