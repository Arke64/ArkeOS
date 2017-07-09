namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class AlreadyDefinedException : CompilationException {
        public AlreadyDefinedException(PositionInfo position, string identifier) : base(position, $"Identifier already defined: '{identifier}'.") { }
    }
}
