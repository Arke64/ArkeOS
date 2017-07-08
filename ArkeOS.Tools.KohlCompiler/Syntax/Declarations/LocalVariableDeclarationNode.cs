namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class LocalVariableDeclarationNode : DeclarationNode {
        public LocalVariableDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier, type) { }
    }
}
