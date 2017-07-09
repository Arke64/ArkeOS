namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ConstDeclarationNode : DeclarationNode {
        public ExpressionStatementNode Value { get; }

        public ConstDeclarationNode(Token identifier, TypeIdentifierNode type, ExpressionStatementNode value) : base(identifier, type) => this.Value = value;
    }
}
