namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ProgramDeclarationNode : SyntaxNode {
        public SyntaxListNode<FunctionDeclarationNode> FunctionDeclarations { get; }
        public SyntaxListNode<VariableDeclarationNode> VariableDeclarations { get; }

        public ProgramDeclarationNode() => (this.FunctionDeclarations, this.VariableDeclarations) = (new SyntaxListNode<FunctionDeclarationNode>(), new SyntaxListNode<VariableDeclarationNode>());
    }
}
