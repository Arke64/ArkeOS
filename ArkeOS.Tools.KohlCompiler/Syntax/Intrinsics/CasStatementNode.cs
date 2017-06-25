namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CasStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public CasStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
