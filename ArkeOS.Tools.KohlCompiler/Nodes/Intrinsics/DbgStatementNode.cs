namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class DbgStatementNode : StatementNode {
        public ArgumentListNode ArgumentList { get; }

        public DbgStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
