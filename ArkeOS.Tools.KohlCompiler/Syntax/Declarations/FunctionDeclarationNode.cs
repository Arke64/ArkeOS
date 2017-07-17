namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FunctionDeclarationNode : TypedDeclarationNode {
        public ArgumentListDeclarationNode ArgumentListDeclaration { get; }
        public StatementBlockNode StatementBlock { get; }

        public FunctionDeclarationNode(Token identifier, TypeIdentifierNode type, ArgumentListDeclarationNode argumentListDeclaration, StatementBlockNode statementBlock) : base(identifier, type) => (this.ArgumentListDeclaration, this.StatementBlock) = (argumentListDeclaration, statementBlock);
    }
}
