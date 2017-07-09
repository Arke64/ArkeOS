namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class StatementBlockNode : SyntaxNode {
        public SyntaxListNode<StatementNode> Statements { get; }
        public SyntaxListNode<LocalVariableDeclarationNode> VariableDeclarations { get; }

        public StatementBlockNode(PositionInfo position) : base(position) => (this.Statements, this.VariableDeclarations) = (new SyntaxListNode<StatementNode>(position), new SyntaxListNode<LocalVariableDeclarationNode>(position));
    }
}
