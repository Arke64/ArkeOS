namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class VariableDeclarationNode : DeclarationNode {
        public ExpressionStatementNode Initializer { get; }

        public VariableDeclarationNode(Token identifier, TypeIdentifierNode type, ExpressionStatementNode initializer) : base(identifier, type) => this.Initializer = initializer;
    }
}
