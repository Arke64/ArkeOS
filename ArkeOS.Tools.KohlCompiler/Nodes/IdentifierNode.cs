namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IdentifierNode : Node {
        public string Identifier { get; }

        public IdentifierNode(Token token) => this.Identifier = token.Value;
    }
}
