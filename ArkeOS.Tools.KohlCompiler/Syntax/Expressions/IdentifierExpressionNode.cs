namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class IdentifierExpressionNode : ExpressionStatementNode {
        public string Identifier { get; }

        protected IdentifierExpressionNode(Token token) => this.Identifier = token.Value;
    }
}
