namespace ArkeOS.Tools.KohlCompiler {
    public struct PositionInfo {
        public string File { get; }
        public int Line { get; }
        public int Column { get; }

        public PositionInfo(string file, int line, int column) => (this.File, this.Line, this.Column) = (file, line, column);
    }
}
