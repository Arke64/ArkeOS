namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class BoolLiteralNode : LiteralExpressionNode {
        public bool Literal { get; }

        public BoolLiteralNode(Token token) => this.Literal = token.Value == "true";
    }
}
