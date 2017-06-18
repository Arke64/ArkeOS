namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class BinaryOperationNode : Node {
        public Node Left { get; }
        public Node Right { get; }
        public OperatorNode Op { get; }

        public BinaryOperationNode(Node left, Node right, OperatorNode op) => (this.Left, this.Right, this.Op) = (left, right, op);
    }
}
