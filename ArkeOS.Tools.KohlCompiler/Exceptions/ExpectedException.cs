namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedException : CompilationException {
        public ExpectedException(PositionInfo position, string unexpected) : base(position, $"Unexpected: '{unexpected}'.") { }
        public ExpectedException(PositionInfo position, TokenType unexpected) : base(position, "Unexpected token: " + unexpected.ToString()) { }
        public ExpectedException(PositionInfo position, TokenClass unexpected) : base(position, "Unexpected token: " + unexpected.ToString()) { }
    }
}
