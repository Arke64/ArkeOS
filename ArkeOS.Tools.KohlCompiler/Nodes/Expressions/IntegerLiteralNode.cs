namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class IntegerLiteralNode : RValueNode {
        public ulong Literal { get; }

        public IntegerLiteralNode(Token token) => this.Literal = ulong.Parse(token.Value);
    }
}
