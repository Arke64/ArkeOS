namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class BinaryExpressionNode : ExpressionStatementNode {
        public ExpressionStatementNode Left { get; }
        public OperatorNode Op { get; }
        public ExpressionStatementNode Right { get; }

        public BinaryExpressionNode(ExpressionStatementNode left, OperatorNode op, ExpressionStatementNode right) => (this.Left, this.Right, this.Op) = (left, right, op);
    }
}
