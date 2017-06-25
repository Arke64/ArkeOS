namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IntegerLiteralNode : RValueNode {
        public ulong Literal { get; }

        public IntegerLiteralNode(Token token) => this.Literal = ulong.Parse(token.Value);
    }
}
