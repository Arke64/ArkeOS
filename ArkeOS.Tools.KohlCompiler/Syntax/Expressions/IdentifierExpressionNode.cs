namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class IdentifierExpressionNode : ExpressionStatementNode {
        public Token Token { get; }
        public string Identifier { get; }

        protected IdentifierExpressionNode(Token token) : base(token.Position) => (this.Token, this.Identifier) = (token, token.Value);
    }
}
