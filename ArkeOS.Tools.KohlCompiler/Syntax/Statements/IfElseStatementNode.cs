namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IfElseStatementNode : IfStatementNode {
        public StatementBlockNode ElseStatementBlock { get; }

        public IfElseStatementNode(ExpressionNode expression, StatementBlockNode statementBlock, StatementBlockNode elseStatementBlock) : base(expression, statementBlock) => this.ElseStatementBlock = elseStatementBlock;
    }
}
