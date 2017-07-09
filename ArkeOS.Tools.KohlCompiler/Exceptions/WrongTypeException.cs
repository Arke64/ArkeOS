namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public sealed class WrongTypeException : CompilationException {
        public WrongTypeException(PositionInfo position, string message) : base(position, $"Wrong type: '{message}'.") { }
    }
}
