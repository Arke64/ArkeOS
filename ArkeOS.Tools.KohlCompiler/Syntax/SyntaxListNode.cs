using System.Collections;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class SyntaxListNode<T> : SyntaxNode, IList<T>, IReadOnlyList<T> where T : SyntaxNode {
        private readonly List<T> items = new List<T>();

        public T this[int index] { get => this.items[index]; set => this.items[index] = value; }

        public int Count => this.items.Count;
        public bool IsReadOnly => ((IList<T>)this.items).IsReadOnly;

        public void Add(T node) => this.items.Add(node);
        public void Clear() => this.items.Clear();
        public bool Contains(T item) => this.items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();
        public int IndexOf(T item) => this.items.IndexOf(item);
        public void Insert(int index, T item) => this.items.Insert(index, item);
        public bool Remove(T item) => this.items.Remove(item);
        public void RemoveAt(int index) => this.items.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();
    }
}
