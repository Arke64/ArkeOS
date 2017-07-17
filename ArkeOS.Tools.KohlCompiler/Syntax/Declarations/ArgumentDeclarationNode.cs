namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class ArgumentDeclarationNode : TypedDeclarationNode {
        public ArgumentDeclarationNode(Token identifier, TypeIdentifierNode type) : base(identifier, type) { }
    }
}
