namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IntegerLiteralNode : LiteralExpressionNode {
        public ulong Literal { get; }

        public IntegerLiteralNode(Token token) => this.Literal = ulong.Parse(token.Value);
    }
}
