using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class CompilationException : Exception {
        public PositionInfo Position { get; }

        protected CompilationException(PositionInfo position) : this(position, string.Empty) { }
        protected CompilationException(PositionInfo position, string message) : base(message) => this.Position = position;
    }
}
