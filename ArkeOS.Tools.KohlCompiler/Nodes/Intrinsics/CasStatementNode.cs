namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CasStatementNode : StatementNode {
        public IdentifierNode A { get; }
        public IdentifierNode B { get; }
        public ValueNode C { get; }

        public CasStatementNode(IdentifierNode a, IdentifierNode b, ValueNode c) => (this.A, this.B, this.C) = (a, b, c);
    }
}
