namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class AlreadyDefinedException : CompilationException {
        public AlreadyDefinedException(PositionInfo position, string message) : base(position, $"Identifier already defined: '{message}'") { }
    }
}
