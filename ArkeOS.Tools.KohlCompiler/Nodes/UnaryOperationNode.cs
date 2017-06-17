namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class UnaryOperationNode : Node {
        public Node Node { get; }
        public Operator Operator { get; }

        public UnaryOperationNode(Node node, Operator op) => (this.Node, this.Operator) = (node, op);

        public static bool IsValidOperator(Operator op) => op == Operator.UnaryMinus || op == Operator.UnaryPlus;
    }
}
