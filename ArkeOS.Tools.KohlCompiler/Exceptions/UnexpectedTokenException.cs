using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedTokenException : Exception {
        public UnexpectedTokenException(PositionInfo position, string unexpected) : base($"Unexpected token in '{position.File}' at {position.Line:N0}:{position.Column:N0}: '{unexpected}'.") { }
        public UnexpectedTokenException(PositionInfo position, TokenType unexpected) : this(position, unexpected.ToString()) { }
    }
}
