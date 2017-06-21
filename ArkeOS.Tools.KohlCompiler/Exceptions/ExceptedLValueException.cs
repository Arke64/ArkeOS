namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExceptedLValueException : CompilationException {
        public ExceptedLValueException(PositionInfo position) : base(position) { }
        public ExceptedLValueException(PositionInfo position, string message) : base(position, message) { }
    }
}
