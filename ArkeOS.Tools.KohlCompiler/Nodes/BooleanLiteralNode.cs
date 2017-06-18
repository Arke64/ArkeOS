namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class BooleanLiteralNode : LiteralNode {
        public bool Literal { get; }

        public BooleanLiteralNode(Token token) => this.Literal = token.Type == TokenType.TrueKeyword;
    }
}
