namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IntegerLiteralNode : RValueNode {
        public long Literal { get; }

        public IntegerLiteralNode(Token token) => this.Literal = long.Parse(token.Value);
    }
}
