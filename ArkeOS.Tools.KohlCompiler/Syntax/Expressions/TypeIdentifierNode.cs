namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class TypeIdentifierNode : IdentifierExpressionNode {
        public SyntaxListNode<TypeIdentifierNode> GenericArguments { get; }

        public TypeIdentifierNode(Token token, SyntaxListNode<TypeIdentifierNode> genericArguments) : base(token) => this.GenericArguments = genericArguments;
    }
}
