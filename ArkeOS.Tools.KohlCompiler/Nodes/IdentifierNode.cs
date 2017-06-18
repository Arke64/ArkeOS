namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IdentifierNode : ExpressionNode {
        public string Identifier { get; }

        public IdentifierNode(Token token) => this.Identifier = token.Value;
    }
}
