namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class IfStatementNode : StatementNode {
        public ExpressionStatementNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public IfStatementNode(ExpressionStatementNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
