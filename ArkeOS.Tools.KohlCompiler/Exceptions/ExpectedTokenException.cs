using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExpectedTokenException : Exception {
        public ExpectedTokenException(string file, int line, int column, string expected) : base($"Expected token in '{file}' at {line:N0}:{column:N0}: '{expected}'.") { }
        public ExpectedTokenException(string file, int line, int column, TokenType expected) : this(file, line, column, expected.ToString()) { }
    }
}
