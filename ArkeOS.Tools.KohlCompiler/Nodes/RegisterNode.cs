using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public class RegisterNode : IdentifierNode {
        public Register Register { get; }

        public RegisterNode(Token token) => this.Register = token.Value.ToEnum<Register>();
    }
}
