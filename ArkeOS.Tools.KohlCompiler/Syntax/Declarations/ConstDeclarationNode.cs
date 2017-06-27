namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ConstDeclarationNode : VariableDeclarationNode {
        public IntegerLiteralNode Value { get; }

        public ConstDeclarationNode(Token identifier, IntegerLiteralNode value) : base(identifier) => this.Value = value;
    }
}
