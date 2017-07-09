namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class DeclarationNode : StatementNode {
        public Token Token { get; }
        public string Identifier { get; }
        public TypeIdentifierNode Type { get; }

        protected DeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier.Position) => (this.Token, this.Identifier, this.Type) = (identifier, identifier.Value, type);
    }
}
