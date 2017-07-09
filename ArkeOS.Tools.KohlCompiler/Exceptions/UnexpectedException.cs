using ArkeOS.Tools.KohlCompiler.Syntax;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class UnexpectedException : CompilationException {
        public UnexpectedException(PositionInfo position, char unexpected) : base(position, $"Unexpected: '{unexpected}'.") { }
        public UnexpectedException(PositionInfo position, string unexpected) : base(position, $"Unexpected: '{unexpected}'.") { }
        public UnexpectedException(PositionInfo position, TokenType unexpected) : base(position, $"Unexpected token: '{unexpected}'.") { }
    }
}
