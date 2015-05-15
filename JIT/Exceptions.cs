using System;
using System.Runtime.Serialization;

namespace ArkeOS.Interpreter {
	public class UnhandledProgramExceptionException : Exception {
		public UnhandledProgramExceptionException() { }
		public UnhandledProgramExceptionException(string message) : base(message) { }
		public UnhandledProgramExceptionException(string message, Exception innerException) : base(message, innerException) { }
		protected UnhandledProgramExceptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	public class InvalidProgramFormatException : Exception {
		public InvalidProgramFormatException() { }
		public InvalidProgramFormatException(string message) : base(message) { }
		public InvalidProgramFormatException(string message, Exception innerException) : base(message, innerException) { }
		protected InvalidProgramFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
