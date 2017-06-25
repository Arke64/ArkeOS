using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public abstract class SyntaxListNode<T> : SyntaxNode where T : SyntaxNode {
        private readonly List<T> items = new List<T>();

        public IReadOnlyList<T> Items => this.items;

        public void Add(T node) => this.items.Add(node);
    }
}
