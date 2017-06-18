namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class NumberLiteralNode : LiteralNode {
        public long Literal { get; }

        public NumberLiteralNode(Token token) => this.Literal = long.Parse(token.Value);
    }
}
