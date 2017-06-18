using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class UnexpectedTokenException : Exception {
        public UnexpectedTokenException(string file, int line, int column, string expected) : base($"Unexpected token in '{file}' at {line:N0}:{column:N0}: '{expected}'.") { }
        public UnexpectedTokenException(string file, int line, int column, TokenType expected) : this(file, line, column, expected.ToString()) { }
    }
}
