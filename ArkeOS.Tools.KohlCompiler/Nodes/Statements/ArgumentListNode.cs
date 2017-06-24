using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ArgumentListNode : Node {
        private List<ExpressionNode> arguments = new List<ExpressionNode>();

        public IReadOnlyList<ExpressionNode> Arguments => this.arguments;

        public void Add(ExpressionNode node) => this.arguments.Add(node);

        public bool Extract(int index, out ExpressionNode arg) {
            if (index < this.arguments.Count) {
                arg = this.arguments[index];
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
