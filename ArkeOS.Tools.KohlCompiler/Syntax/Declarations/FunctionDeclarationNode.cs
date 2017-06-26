namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FunctionDeclarationNode : DeclarationNode {
        public ArgumentListDeclarationNode ArgumentListDeclaration { get; }
        public StatementBlockNode StatementBlock { get; }

        public FunctionDeclarationNode(Token identifier, ArgumentListDeclarationNode argumentListDeclaration, StatementBlockNode statementBlock) : base(identifier) => (this.ArgumentListDeclaration, this.StatementBlock) = (argumentListDeclaration, statementBlock);
    }
}
