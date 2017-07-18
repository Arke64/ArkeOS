namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class MemberAccessIdentifierNode : IdentifierExpressionNode {
        public IdentifierExpressionNode Member { get; }

        public MemberAccessIdentifierNode(Token token, IdentifierExpressionNode member) : base(token) => this.Member = member;
    }
}
