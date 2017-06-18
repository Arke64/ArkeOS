using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedTokenException : Exception {
        public UnexpectedTokenException(PositionInfo position, string expected) : base($"Unexpected token in '{position.File}' at {position.Line:N0}:{position.Column:N0}: '{expected}'.") { }
        public UnexpectedTokenException(PositionInfo position, TokenType expected) : this(position, expected.ToString()) { }
    }
}
