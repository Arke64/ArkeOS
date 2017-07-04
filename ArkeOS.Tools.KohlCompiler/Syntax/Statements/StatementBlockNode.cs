namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class StatementBlockNode : SyntaxNode {
        public SyntaxListNode<StatementNode> Statements { get; }
        public SyntaxListNode<LocalVariableDeclarationNode> VariableDeclarations { get; }

        public StatementBlockNode() => (this.Statements, this.VariableDeclarations) = (new SyntaxListNode<StatementNode>(), new SyntaxListNode<LocalVariableDeclarationNode>());
    }
}
