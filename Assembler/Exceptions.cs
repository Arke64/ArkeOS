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
}
