namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ConstDeclarationNode : DeclarationNode {
        public IntegerLiteralNode Value { get; }

        public ConstDeclarationNode(Token identifier, IntegerLiteralNode value) : base(identifier) => this.Value = value;
        public ConstDeclarationNode(string identifier, IntegerLiteralNode value) : base(identifier) => this.Value = value;
    }
}
