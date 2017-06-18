using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ArgumentListNode : Node {
        private List<ExpressionNode> arguments = new List<ExpressionNode>();

        public IReadOnlyList<ExpressionNode> Arguments => this.arguments;

        public void Add(ExpressionNode node) => this.arguments.Add(node);
    }
}
