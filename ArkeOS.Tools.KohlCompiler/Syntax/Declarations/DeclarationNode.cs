namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class DeclarationNode : StatementNode {
        public Token Token { get; }
        public string Identifier { get; }

        protected DeclarationNode(Token identifier) : base(identifier.Position) => (this.Token, this.Identifier) = (identifier, identifier.Value);
    }
}
