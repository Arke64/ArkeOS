namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class MemberDereferenceIdentifierNode : MemberAccessIdentifierNode {
        public MemberDereferenceIdentifierNode(Token token, IdentifierExpressionNode member) : base(token, member) { }
    }
}
