namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class WrongNumberOfArgumentsException : CompilationException {
        public WrongNumberOfArgumentsException(PositionInfo position, string identifier) : base(position, identifier) { }
    }
}
