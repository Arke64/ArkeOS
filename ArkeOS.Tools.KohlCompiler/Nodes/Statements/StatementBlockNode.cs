using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class StatementBlockNode : Node {
        private List<StatementNode> statements = new List<StatementNode>();

        public IReadOnlyList<StatementNode> Statements => this.statements;

        public void Add(StatementNode node) => this.statements.Add(node);
    }
}
