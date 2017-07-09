using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public abstract class CompilationException : Exception {
        public PositionInfo Position { get; }

        protected CompilationException(PositionInfo position, string message) : base(message) => this.Position = position;
    }
}
