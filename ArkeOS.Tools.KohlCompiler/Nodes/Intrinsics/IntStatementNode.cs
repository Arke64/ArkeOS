namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class IntStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public IntStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
