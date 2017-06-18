namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IntStatementNode : StatementNode {
        public ExpressionNode A { get; }
        public ExpressionNode B { get; }
        public ExpressionNode C { get; }

        public IntStatementNode(ExpressionNode a, ExpressionNode b, ExpressionNode c) => (this.A, this.B, this.C) = (a, b, c);
    }
}
