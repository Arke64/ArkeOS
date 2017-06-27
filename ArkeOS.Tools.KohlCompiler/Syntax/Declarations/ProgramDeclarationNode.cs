namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ProgramDeclarationNode : SyntaxNode {
        public SyntaxListNode<FunctionDeclarationNode> FunctionDeclarations { get; }
        public SyntaxListNode<VariableDeclarationNode> VariableDeclarations { get; }
        public SyntaxListNode<ConstDeclarationNode> ConstDeclarations { get; }

        public ProgramDeclarationNode() => (this.FunctionDeclarations, this.VariableDeclarations, this.ConstDeclarations) = (new SyntaxListNode<FunctionDeclarationNode>(), new SyntaxListNode<VariableDeclarationNode>(), new SyntaxListNode<ConstDeclarationNode>());
    }
}
