namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class BoolLiteralNode : LiteralNode {
        public bool Literal { get; }

        public BoolLiteralNode(Token token) => this.Literal = token.Value == "true";
    }
}
