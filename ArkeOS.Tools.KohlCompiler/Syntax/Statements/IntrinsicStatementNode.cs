namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class IntrinsicStatementNode : StatementNode {
        public ArgumentListNode ArgumentList { get; }

        public IntrinsicStatementNode() : this(new ArgumentListNode()) { }
        public IntrinsicStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}
