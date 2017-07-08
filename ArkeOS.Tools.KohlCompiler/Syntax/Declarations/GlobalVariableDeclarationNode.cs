namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class GlobalVariableDeclarationNode : DeclarationNode {
        public GlobalVariableDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier, type) { }
    }
}
