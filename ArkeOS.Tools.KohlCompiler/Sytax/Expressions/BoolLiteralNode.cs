namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class BoolLiteralNode : RValueNode {
        public bool Literal { get; }

        public BoolLiteralNode(Token token) => this.Literal = token.Value == "true";
    }
}
