using System;
using System.Runtime.Serialization;

namespace ArkeOS.Assembler {
    public class InvalidInstructionException : Exception {
        public InvalidInstructionException() { }
        public InvalidInstructionException(string message) : base(message) { }
        public InvalidInstructionException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidInstructionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class InvalidParameterException : Exception {
        public InvalidParameterException() { }
        public InvalidParameterException(string message) : base(message) { }
        public InvalidParameterException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidParameterException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class InvalidDirectiveException : Exception {
        public InvalidDirectiveException() { }
        public InvalidDirectiveException(string message) : base(message) { }
        public InvalidDirectiveException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidDirectiveException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class VariableNotFoundException : Exception {
        public VariableNotFoundException() { }
        public VariableNotFoundException(string message) : base(message) { }
        public VariableNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        protected VariableNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class FunctionNotFoundException : Exception {
        public FunctionNotFoundException() { }
        public FunctionNotFoundException(string message) : base(message) { }
        public FunctionNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        protected FunctionNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
