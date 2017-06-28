namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class VariableDeclarationWithInitializerNode : VariableDeclarationNode {
        public ExpressionStatementNode Initializer { get; }

        public VariableDeclarationWithInitializerNode(Token identifier, ExpressionStatementNode initializer) : base(identifier) => this.Initializer = initializer;
    }
}
