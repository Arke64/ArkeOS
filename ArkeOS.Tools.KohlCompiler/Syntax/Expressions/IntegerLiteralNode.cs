namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IntegerLiteralNode : LiteralExpressionNode {
        public ulong Literal { get; }

        public IntegerLiteralNode(Token token) : this(token.Position, ulong.Parse(token.Value)) { }
        public IntegerLiteralNode(PositionInfo position, ulong literal) : base(position) => this.Literal = literal;
    }
}
