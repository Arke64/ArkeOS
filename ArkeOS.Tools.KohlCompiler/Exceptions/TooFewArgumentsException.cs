using System;

namespace ArkeOS.Tools.KohlCompiler.Exceptions {
    public class TooFewArgumentsException : Exception {
        public TooFewArgumentsException() { }
        public TooFewArgumentsException(string message) : base(message) { }
    }
}
