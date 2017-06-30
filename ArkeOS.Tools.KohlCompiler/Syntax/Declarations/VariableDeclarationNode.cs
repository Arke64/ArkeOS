namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class VariableDeclarationNode : DeclarationNode {
        public VariableDeclarationNode(Token identifier) : base(identifier) { }
        public VariableDeclarationNode(string identifier) : base(identifier) { }
    }
}
