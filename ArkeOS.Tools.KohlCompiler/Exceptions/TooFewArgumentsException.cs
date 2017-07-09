namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class TooFewArgumentsException : CompilationException {
        public TooFewArgumentsException(PositionInfo position, string identifier) : base(position, identifier) { }
    }
}
