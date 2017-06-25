namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class UnaryExpressionNode : ExpressionStatementNode {
        public OperatorNode Op { get; }
        public ExpressionStatementNode Expression { get; }

        public UnaryExpressionNode(OperatorNode op, ExpressionStatementNode expression) => (this.Expression, this.Op) = (expression, op);
    }
}
