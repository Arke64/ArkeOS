using ArkeOS.Tools.KohlCompiler.Syntax;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class ExpectedException : CompilationException {
        public ExpectedException(PositionInfo position, string expected) : base(position, $"Expected: '{expected}'.") { }
        public ExpectedException(PositionInfo position, TokenType expected) : base(position, $"Expected token: '{expected}'.") { }
        public ExpectedException(PositionInfo position, TokenClass expected) : base(position, $"Expected token: '{expected}'.") { }
    }
}
