namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class AssignmentNode : Node {
        public IdentifierNode Target { get; }
        public Node Value { get; }

        public AssignmentNode(IdentifierNode identifier, Node value) => (this.Target, this.Value) = (identifier, value);
    }
}
