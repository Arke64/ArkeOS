namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class DbgStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public DbgStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
