namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class LocalVariableDeclarationWithInitializerNode : LocalVariableDeclarationNode {
        public ExpressionStatementNode Initializer { get; }

        public LocalVariableDeclarationWithInitializerNode(Token identifier, TypeIdentifierNode type, ExpressionStatementNode initializer) : base(identifier, type) => this.Initializer = initializer;
    }
}
