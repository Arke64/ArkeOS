namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class VariableIdentifierNode : IdentifierExpressionNode {
        public VariableIdentifierNode(Token token) : base(token) { }
        public VariableIdentifierNode(string identifier) : base(identifier) { }
    }
}
