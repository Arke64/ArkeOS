namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class SyntaxNode {
        public PositionInfo Position { get; }

        protected SyntaxNode(PositionInfo position) => this.Position = position;
    }
}
