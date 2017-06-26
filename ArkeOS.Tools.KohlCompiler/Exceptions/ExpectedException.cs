namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedException : CompilationException {
        public ExpectedException(PositionInfo position, string expected) : base(position, $"Expected: '{expected}'.") { }
        public ExpectedException(PositionInfo position, TokenType expected) : base(position, "Expected token: " + expected.ToString()) { }
        public ExpectedException(PositionInfo position, TokenClass expected) : base(position, "Expected token: " + expected.ToString()) { }
    }
}
