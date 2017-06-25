using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public sealed class RegisterNode : LValueNode {
        public Register Register { get; }

        public RegisterNode(Token token) => this.Register = token.Value.ToEnum<Register>();
    }
}
