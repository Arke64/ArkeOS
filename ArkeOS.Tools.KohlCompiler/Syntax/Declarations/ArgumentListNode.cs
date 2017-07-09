namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class ArgumentListNode : SyntaxListNode<ExpressionStatementNode> {
        public ArgumentListNode(PositionInfo position) : base(position) { }

        public bool Extract(int index, out ExpressionStatementNode arg) {
            if (index < this.Count) {
                arg = this[index];
                return true;
            }
            else {
                arg = default(ExpressionStatementNode);
                return false;
            }
        }

        public bool Extract(out ExpressionStatementNode arg0) => this.Extract(0, out arg0);
        public bool Extract(out ExpressionStatementNode arg0, out ExpressionStatementNode arg1) { arg0 = arg1 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1); }
        public bool Extract(out ExpressionStatementNode arg0, out ExpressionStatementNode arg1, out ExpressionStatementNode arg2) { arg0 = arg1 = arg2 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1) && this.Extract(2, out arg2); }
    }
}
