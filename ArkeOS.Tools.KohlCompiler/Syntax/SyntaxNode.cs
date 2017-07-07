namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class SyntaxNode {
        public PositionInfo Position { get; }

        protected SyntaxNode() : this(default(PositionInfo)) { }
        protected SyntaxNode(PositionInfo position) => this.Position = position;
    }
}
