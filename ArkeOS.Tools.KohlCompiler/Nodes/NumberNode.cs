namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class NumberNode : Node {
        public long Number { get; }

        public NumberNode(string number) => this.Number = long.Parse(number);
    }
}
