namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class GlobalVariableDeclarationNode : DeclarationNode {
        public GlobalVariableDeclarationNode(Token identifier) : base(identifier) { }
        public GlobalVariableDeclarationNode(string identifier) : base(identifier) { }
    }
}
