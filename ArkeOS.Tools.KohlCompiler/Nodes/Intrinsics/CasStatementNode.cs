namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CasStatementNode : StatementNode {
        public IdentifierNode A { get; }
        public IdentifierNode B { get; }
        public ExpressionNode C { get; }

        public CasStatementNode(IdentifierNode a, IdentifierNode b, ExpressionNode c) => (this.A, this.B, this.C) = (a, b, c);
    }
}
