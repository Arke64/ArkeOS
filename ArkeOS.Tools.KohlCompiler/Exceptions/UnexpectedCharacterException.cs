using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedCharacterException : Exception {
        public UnexpectedCharacterException(PositionInfo position, char unexpected) : base($"Unexpected character in '{position.File}' at {position.Line:N0}:{position.Column:N0}: '{unexpected}'.") { }
    }
}
