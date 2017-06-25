namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class CpyStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public CpyStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
