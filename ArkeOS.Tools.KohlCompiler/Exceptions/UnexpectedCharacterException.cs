namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedCharacterException : CompilationException {
        public UnexpectedCharacterException(PositionInfo position, char unexpected) : base(position, $"Unexpected character: '{unexpected}'.") { }
    }
}
