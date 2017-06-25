namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class BoolLiteralNode : RValueNode {
        public bool Literal { get; }

        public BoolLiteralNode(Token token) => this.Literal = token.Value == "true";
    }
}
