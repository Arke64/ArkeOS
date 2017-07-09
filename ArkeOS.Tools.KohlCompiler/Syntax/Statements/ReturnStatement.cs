namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ReturnStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }

        public ReturnStatementNode(PositionInfo position, ExpressionStatementNode expression) : base(position) => this.Expression = expression;
    }
}
