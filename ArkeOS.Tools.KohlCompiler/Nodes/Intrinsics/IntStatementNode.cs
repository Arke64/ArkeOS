namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class IntStatementNode : StatementNode {
        public ArgumentListNode ArgumentList { get; }

        public IntStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
