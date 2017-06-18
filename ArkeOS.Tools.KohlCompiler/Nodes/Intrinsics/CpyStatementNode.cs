namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CpyStatementNode : StatementNode {
        public ArgumentListNode ArgumentList { get; }

        public CpyStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
