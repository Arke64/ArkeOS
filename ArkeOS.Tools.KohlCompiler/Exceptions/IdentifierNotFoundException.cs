namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class IdentifierNotFoundException : CompilationException {
        public IdentifierNotFoundException(PositionInfo position, string message) : base(position, message) { }
    }
}
