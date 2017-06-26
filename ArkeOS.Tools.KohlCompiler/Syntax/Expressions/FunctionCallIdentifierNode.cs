namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class FunctionCallIdentifierNode : IdentifierExpressionNode {
        public ArgumentListNode ArgumentList { get; }

        public FunctionCallIdentifierNode(Token token, ArgumentListNode argumentList) : base(token) => this.ArgumentList = argumentList;
    }
}
