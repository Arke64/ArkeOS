namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class VariableDeclarationAndInitializationNode : VariableDeclarationNode {
        public ExpressionStatementNode Initializer { get; }

        public VariableDeclarationAndInitializationNode(Token identifier, TypeIdentifierNode type, ExpressionStatementNode initializer) : base(identifier, type) => this.Initializer = initializer;
    }
}
