using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedTokenException : Exception {
        public ExpectedTokenException(PositionInfo position, string expected) : base($"Expected token in '{position.File}' at {position.Line:N0}:{position.Column:N0}: '{expected}'.") { }
        public ExpectedTokenException(PositionInfo position, TokenType expected) : this(position, expected.ToString()) { }
    }
}
