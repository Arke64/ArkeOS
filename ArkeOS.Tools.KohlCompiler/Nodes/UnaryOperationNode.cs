namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class UnaryOperationNode : Node {
        public Node Node { get; }
        public OperatorNode Op { get; }

        public UnaryOperationNode(Node node, OperatorNode op) => (this.Node, this.Op) = (node, op);
    }
}
