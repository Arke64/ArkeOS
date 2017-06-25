namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FunctionDeclarationNode : DeclarationNode {
        public StatementBlockNode StatementBlock { get; }

        public FunctionDeclarationNode(Token identifier, StatementBlockNode statementBlock) : base(identifier) => this.StatementBlock = statementBlock;
    }
}
