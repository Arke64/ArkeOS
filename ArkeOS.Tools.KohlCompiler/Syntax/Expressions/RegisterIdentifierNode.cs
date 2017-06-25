using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class RegisterIdentifierNode : IdentifierExpressionNode {
        public Register Register { get; }

        public RegisterIdentifierNode(Token token) : base(token) => this.Register = token.Value.ToEnum<Register>();
    }
}
