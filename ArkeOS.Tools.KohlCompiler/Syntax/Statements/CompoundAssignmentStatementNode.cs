namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CompoundAssignmentStatementNode : AssignmentStatementNode {
        public OperatorNode Op { get; }

        public CompoundAssignmentStatementNode(IdentifierNode identifier, OperatorNode op, ExpressionNode expression) : base(identifier, expression) => this.Op = op;
    }
}
