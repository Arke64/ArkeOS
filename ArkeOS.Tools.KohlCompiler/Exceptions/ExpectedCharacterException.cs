using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedCharacterException : Exception {
        public ExpectedCharacterException(PositionInfo position, char expected) : base($"Expected character in '{position.File}' at {position.Line:N0}:{position.Column:N0}: '{expected}'.") { }
    }
}
