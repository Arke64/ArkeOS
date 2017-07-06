namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IntegerLiteralNode : LiteralExpressionNode {
        public ulong Literal { get; }

        public IntegerLiteralNode(Token token) : this(ulong.Parse(token.Value)) { }
        public IntegerLiteralNode(ulong literal) => this.Literal = literal;
    }
}
