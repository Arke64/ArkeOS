using System.Collections.Generic;
namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ProgramNode : Node {
        private List<AssignmentNode> assignments = new List<AssignmentNode>();

        public IReadOnlyList<AssignmentNode> Assignments => this.assignments;

        public void Add(AssignmentNode node) => this.assignments.Add(node);
    }
}
