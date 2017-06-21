namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedTokenException : CompilationException {
        public ExpectedTokenException(PositionInfo position, string expected) : base(position, $"Expected token: '{expected}'.") { }
        public ExpectedTokenException(PositionInfo position, TokenType expected) : this(position, expected.ToString()) { }
    }
}
