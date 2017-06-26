namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class IdentifierNotFoundException : CompilationException {
        public IdentifierNotFoundException(PositionInfo position, string identifier) : base(position, $"Identifier not found: '{identifier}'") { }
    }
}
