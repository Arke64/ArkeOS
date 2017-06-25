namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IdentifierNode {
        public string Identifier { get; }

        public IdentifierNode(Token token) => this.Identifier = token.Value;
    }
}
