namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class WrongTypeException : CompilationException {
        public WrongTypeException(PositionInfo position, string identifier) : base(position, $"Wrong type: '{identifier}'") { }
    }
}
