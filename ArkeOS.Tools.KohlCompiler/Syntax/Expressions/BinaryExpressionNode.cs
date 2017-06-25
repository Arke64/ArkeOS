namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class BinaryExpressionNode : ExpressionNode {
        public ExpressionNode Left { get; }
        public OperatorNode Op { get; }
        public ExpressionNode Right { get; }

        public BinaryExpressionNode(ExpressionNode left, OperatorNode op, ExpressionNode right) => (this.Left, this.Right, this.Op) = (left, right, op);
    }
}
