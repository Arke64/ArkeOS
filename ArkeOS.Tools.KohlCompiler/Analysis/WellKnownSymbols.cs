namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public static class WellKnownSymbol {
        public static TypeSymbol Word { get; } = new TypeSymbol("word", 1);
        public static TypeSymbol Bool { get; } = new TypeSymbol("bool", 1);
    }
}
