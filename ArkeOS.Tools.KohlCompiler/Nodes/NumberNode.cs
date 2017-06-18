namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class NumberNode : ValueNode {
        public long Number { get; }

        public NumberNode(Token token) => this.Number = long.Parse(token.Value);
    }
}
