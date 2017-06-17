namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IdentifierNode : Node {
        public string Identifier { get; }

        public IdentifierNode(string identifier) => this.Identifier = identifier;
    }
}
