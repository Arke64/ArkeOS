namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class DeclarationNode : SyntaxNode {
        public string Identifier { get; }

        protected DeclarationNode(Token token) => this.Identifier = token.Value;
    }
}
