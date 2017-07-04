namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class LocalVariableDeclarationWithInitializerNode : LocalVariableDeclarationNode {
        public ExpressionStatementNode Initializer { get; }

        public LocalVariableDeclarationWithInitializerNode(Token identifier, ExpressionStatementNode initializer) : base(identifier) => this.Initializer = initializer;
    }
}
