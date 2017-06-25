using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public abstract class ListNode<T> : Node where T : Node {
        private readonly List<T> items = new List<T>();

        public IReadOnlyList<T> Items => this.items;

        public void Add(T node) => this.items.Add(node);
    }
}
