namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IntegerLiteralNode : LiteralNode {
        public long Literal { get; }

        public IntegerLiteralNode(Token token) => this.Literal = long.Parse(token.Value);
    }
}
