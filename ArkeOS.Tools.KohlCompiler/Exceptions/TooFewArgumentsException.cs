namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class TooFewArgumentsException : CompilationException {
        public TooFewArgumentsException(PositionInfo position) : base(position) { }
        public TooFewArgumentsException(PositionInfo position, string message) : base(position, message) { }
    }
}
