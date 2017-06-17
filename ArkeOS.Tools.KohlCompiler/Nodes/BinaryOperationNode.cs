namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class BinaryOperationNode : Node {
        public Node Left { get; }
        public Node Right { get; }
        public Operator Operator { get; }

        public BinaryOperationNode(Node left, Node right, Operator op) => (this.Left, this.Right, this.Operator) = (left, right, op);
    }
}
