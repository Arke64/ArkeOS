namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class StructDeclarationNode : DeclarationNode {
        public SyntaxListNode<VariableDeclarationNode> Variables { get; }

        public StructDeclarationNode(Token identifier, SyntaxListNode<VariableDeclarationNode> variables) : base(identifier) => this.Variables = variables;
    }
}
