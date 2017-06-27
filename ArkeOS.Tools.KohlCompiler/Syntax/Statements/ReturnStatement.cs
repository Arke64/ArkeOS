namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ReturnStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }

        public ReturnStatementNode(ExpressionStatementNode expression) => this.Expression = expression;
    }
}
