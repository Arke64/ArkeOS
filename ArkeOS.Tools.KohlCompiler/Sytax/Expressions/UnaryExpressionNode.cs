namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class UnaryExpressionNode : ExpressionNode {
        public OperatorNode Op { get; }
        public ExpressionNode Expression { get; }

        public UnaryExpressionNode(OperatorNode op, ExpressionNode expression) => (this.Expression, this.Op) = (expression, op);
    }
}
