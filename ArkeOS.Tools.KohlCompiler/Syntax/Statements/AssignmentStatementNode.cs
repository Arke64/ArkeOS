namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class AssignmentStatementNode : StatementNode {
        public IdentifierNode Identifier { get; }
        public ExpressionNode Expression { get; }

        public AssignmentStatementNode(IdentifierNode identifier, ExpressionNode expression) => (this.Identifier, this.Expression) = (identifier, expression);
    }
}
