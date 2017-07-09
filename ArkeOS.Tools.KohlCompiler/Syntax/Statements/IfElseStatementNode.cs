namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class IfElseStatementNode : IfStatementNode {
        public StatementBlockNode ElseStatementBlock { get; }

        public IfElseStatementNode(PositionInfo position, ExpressionStatementNode expression, StatementBlockNode statementBlock, StatementBlockNode elseStatementBlock) : base(position, expression, statementBlock) => this.ElseStatementBlock = elseStatementBlock;
    }
}
