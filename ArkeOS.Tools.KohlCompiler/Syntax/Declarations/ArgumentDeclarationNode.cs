namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ArgumentDeclarationNode : DeclarationNode {
        public ArgumentDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier, type) { }
    }
}
