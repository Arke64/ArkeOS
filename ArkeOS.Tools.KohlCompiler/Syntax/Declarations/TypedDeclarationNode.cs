namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class TypedDeclarationNode : DeclarationNode {
        public TypeIdentifierNode Type { get; }

        protected TypedDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier) => this.Type = type;
    }
}
