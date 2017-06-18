namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ProgramNode : Node {
        public StatementBlockNode Block { get; }

        public ProgramNode(StatementBlockNode block) => this.Block = block;
    }
}
