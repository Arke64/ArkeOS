using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class TypeIdentifierNode : IdentifierExpressionNode {
        public IReadOnlyCollection<TypeIdentifierNode> GenericArguments { get; }

        public TypeIdentifierNode(Token token) : this(token, new List<TypeIdentifierNode>()) { }
        public TypeIdentifierNode(Token token, IReadOnlyCollection<TypeIdentifierNode> genericArguments) : base(token) => this.GenericArguments = genericArguments;
    }
}
