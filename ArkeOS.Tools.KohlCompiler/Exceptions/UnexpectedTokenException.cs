namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedTokenException : CompilationException {
        public UnexpectedTokenException(PositionInfo position, string unexpected) : base(position, $"Unexpected token: '{unexpected}'.") { }
        public UnexpectedTokenException(PositionInfo position, TokenType unexpected) : this(position, unexpected.ToString()) { }
    }
}
