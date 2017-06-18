using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class ExceptedLValueException : Exception {
        public ExceptedLValueException() { }
        public ExceptedLValueException(string message) : base(message) { }
    }
}
