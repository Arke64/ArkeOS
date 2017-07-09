namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ProgramDeclarationNode : SyntaxNode {
        public SyntaxListNode<FunctionDeclarationNode> FunctionDeclarations { get; }
        public SyntaxListNode<GlobalVariableDeclarationNode> VariableDeclarations { get; }
        public SyntaxListNode<ConstDeclarationNode> ConstDeclarations { get; }

        public ProgramDeclarationNode(PositionInfo position) : base(position) => (this.FunctionDeclarations, this.VariableDeclarations, this.ConstDeclarations) = (new SyntaxListNode<FunctionDeclarationNode>(position), new SyntaxListNode<GlobalVariableDeclarationNode>(position), new SyntaxListNode<ConstDeclarationNode>(position));
    }
}
