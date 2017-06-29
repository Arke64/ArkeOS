namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class DeclarationNode : StatementNode {
        public Token Token { get; }
        public string Identifier { get; }

        protected DeclarationNode(Token token) => (this.Token, this.Identifier) = (token, token.Value);
    }
}
