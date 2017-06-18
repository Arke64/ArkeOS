namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class ProgramNode : Node {
        public StatementBlockNode StatementBlock { get; }

        public ProgramNode(StatementBlockNode statementBlock) => this.StatementBlock = statementBlock;
    }
}
