namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class StructDeclarationNode : DeclarationNode {
        public SyntaxListNode<VariableDeclarationNode> Members { get; }

        public StructDeclarationNode(Token identifier, SyntaxListNode<VariableDeclarationNode> members) : base(identifier) => this.Members = members;
    }
}
