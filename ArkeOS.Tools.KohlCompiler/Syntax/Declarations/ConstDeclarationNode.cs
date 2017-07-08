namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ConstDeclarationNode : DeclarationNode {
        public IntegerLiteralNode Value { get; }

        public ConstDeclarationNode(Token identifier, TypeIdentifierNode type, IntegerLiteralNode value) : base(identifier, type) => this.Value = value;
    }
}
