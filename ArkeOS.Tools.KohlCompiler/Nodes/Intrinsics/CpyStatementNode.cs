namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CpyStatementNode : StatementNode {
        public ExpressionNode A { get; }
        public ExpressionNode B { get; }
        public ExpressionNode C { get; }

        public CpyStatementNode(ExpressionNode a, ExpressionNode b, ExpressionNode c) => (this.A, this.B, this.C) = (a, b, c);
    }
}
