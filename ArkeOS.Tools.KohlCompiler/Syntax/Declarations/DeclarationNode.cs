namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class DeclarationNode : StatementNode {
        public string Identifier { get; }

        protected DeclarationNode(Token token) => this.Identifier = token.Value;
    }
}
