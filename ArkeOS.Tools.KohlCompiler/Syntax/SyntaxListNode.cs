using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class SyntaxListNode<T> : SyntaxNode where T : SyntaxNode {
        private readonly List<T> items = new List<T>();

        public IReadOnlyList<T> Items => this.items;

        public void Add(T node) => this.items.Add(node);

        public bool TryGetIndex(Func<T, bool> predicate, out ulong result) {
            result = (ulong)this.items.TakeWhile(v => !predicate(v)).Count();

            return result != (ulong)this.items.Count;
        }
    }
}
