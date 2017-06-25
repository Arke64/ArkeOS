namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class CasStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public CasStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
