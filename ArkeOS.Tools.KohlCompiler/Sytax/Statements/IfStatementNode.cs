﻿namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class IfStatementNode : BlockStatementNode {
        public ExpressionNode Expression { get; }
        public StatementBlockNode StatementBlock { get; }

        public IfStatementNode(ExpressionNode expression, StatementBlockNode statementBlock) => (this.Expression, this.StatementBlock) = (expression, statementBlock);
    }
}
