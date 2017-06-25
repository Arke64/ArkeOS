namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FunctionCallStatementNode : StatementNode {
        public IdentifierNode Identifier { get; }

        public FunctionCallStatementNode(IdentifierNode identifier) => this.Identifier = identifier;
    }
}
