namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class VariableDeclarationNode : TypedDeclarationNode {
        public VariableDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier, type) { }
    }
}
