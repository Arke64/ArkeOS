namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class DbgStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public DbgStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
