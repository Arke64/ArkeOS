namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class XchgStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public XchgStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
