namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class XchgStatementNode : StatementNode {
        public IdentifierNode A { get; }
        public IdentifierNode B { get; }

        public XchgStatementNode(IdentifierNode a, IdentifierNode b) => (this.A, this.B) = (a, b);
    }
}
