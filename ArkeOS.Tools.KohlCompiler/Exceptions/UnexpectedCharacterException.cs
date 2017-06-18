using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedCharacterException : Exception {
        public UnexpectedCharacterException(string file, int line, int column, char unexpected) : base($"Unexpected character in '{file}' at {line:N0}:{column:N0}: '{unexpected}'.") { }
    }
}
