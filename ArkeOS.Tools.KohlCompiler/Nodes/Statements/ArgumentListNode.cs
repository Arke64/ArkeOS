namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ArgumentListNode : ListNode<ExpressionNode> {
        public bool Extract(int index, out ExpressionNode arg) {
            if (index < this.Items.Count) {
                arg = this.Items[index];
                return true;
            }
            else {
                arg = default(ExpressionNode);
                return false;
            }
        }

        public bool Extract(out ExpressionNode arg0) => this.Extract(0, out arg0);
        public bool Extract(out ExpressionNode arg0, out ExpressionNode arg1) { arg0 = arg1 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1); }
        public bool Extract(out ExpressionNode arg0, out ExpressionNode arg1, out ExpressionNode arg2) { arg0 = arg1 = arg2 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1) && this.Extract(2, out arg2); }
    }
}
