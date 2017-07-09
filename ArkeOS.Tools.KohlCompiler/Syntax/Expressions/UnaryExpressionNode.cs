namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class UnaryExpressionNode : ExpressionStatementNode {
        public OperatorNode Op { get; }
        public ExpressionStatementNode Expression { get; }

        public UnaryExpressionNode(PositionInfo position, OperatorNode op, ExpressionStatementNode expression) : base(position) => (this.Op, this.Expression) = (op, expression);
    }
}
