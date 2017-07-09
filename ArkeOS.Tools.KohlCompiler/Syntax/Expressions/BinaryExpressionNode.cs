namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class BinaryExpressionNode : ExpressionStatementNode {
        public ExpressionStatementNode Left { get; }
        public OperatorNode Op { get; }
        public ExpressionStatementNode Right { get; }

        public BinaryExpressionNode(PositionInfo position, ExpressionStatementNode left, OperatorNode op, ExpressionStatementNode right) : base(position) => (this.Left, this.Right, this.Op) = (left, right, op);
    }
}
