namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class IdentifierExpressionNode : ExpressionStatementNode {
        public Token Token { get; }
        public string Identifier { get; }

        protected IdentifierExpressionNode(string identifier) : this(new Token(TokenType.Identifier, identifier)) { }
        protected IdentifierExpressionNode(Token token) => (this.Token, this.Identifier) = (token, token.Value);
    }
}
