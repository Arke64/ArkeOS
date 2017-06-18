namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CpyStatementNode : StatementNode {
        public ValueNode A { get; }
        public ValueNode B { get; }
        public ValueNode C { get; }

        public CpyStatementNode(ValueNode a, ValueNode b, ValueNode c) => (this.A, this.B, this.C) = (a, b, c);
    }
}
