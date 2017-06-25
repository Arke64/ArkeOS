namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class CpyStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public CpyStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
