namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class StatementBlockNode : SyntaxNode {
        public SyntaxListNode<StatementNode> Statements { get; }
        public SyntaxListNode<VariableDeclarationNode> VariableDeclarations { get; }

        public StatementBlockNode(PositionInfo position) : base(position) => (this.Statements, this.VariableDeclarations) = (new SyntaxListNode<StatementNode>(position), new SyntaxListNode<VariableDeclarationNode>(position));
    }
}
