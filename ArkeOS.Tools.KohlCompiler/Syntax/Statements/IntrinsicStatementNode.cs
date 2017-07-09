namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class IntrinsicStatementNode : StatementNode {
        public ArgumentListNode ArgumentList { get; }

        public IntrinsicStatementNode(PositionInfo position) : this(position, new ArgumentListNode(position)) { }
        public IntrinsicStatementNode(PositionInfo position, ArgumentListNode argumentList) : base(position) => this.ArgumentList = argumentList;
    }
}
