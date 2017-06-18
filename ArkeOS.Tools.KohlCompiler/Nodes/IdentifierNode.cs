namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IdentifierNode : ValueNode {
        public string Identifier { get; }

        public IdentifierNode(Token token) => this.Identifier = token.Value;
    }
}
