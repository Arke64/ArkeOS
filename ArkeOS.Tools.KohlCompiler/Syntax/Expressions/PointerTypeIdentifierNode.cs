namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class PointerTypeIdentifierNode : TypeIdentifierNode {
        public TypeIdentifierNode Target { get; }

        public PointerTypeIdentifierNode(TypeIdentifierNode target) : base(target.Token) => this.Target = target;
    }
}
