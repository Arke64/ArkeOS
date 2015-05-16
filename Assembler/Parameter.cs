using System;
using ArkeOS.Executable;

namespace ArkeOS.Assembler {
	public class Parameter {
		public bool IsValid { get; }
		public bool IsRegister { get; }
		public Register Register { get; }
		public ulong Literal { get; }

		public Parameter(string value) {
			this.IsValid = true;

			try {
				if (value.IndexOf("0x") == 0) {
					this.Literal = Convert.ToUInt64(value, 16);
					this.IsRegister = false;
				}
				else if (value.IndexOf("0o") == 0) {
					this.Literal = Convert.ToUInt64(value, 8);
					this.IsRegister = false;
				}
				else if (value.IndexOf("0b") == 0) {
					this.Literal = Convert.ToUInt64(value, 2);
					this.IsRegister = false;
				}
				else if (value.IndexOf("R") == 0) {
					this.Register = (Register)Enum.Parse(typeof(Register), value);
					this.IsRegister = true;
				}
				else {
					this.Literal = Convert.ToUInt64(value, 10);
					this.IsRegister = false;
				}
			}
			catch (FormatException) {
				this.IsValid = false;
			}
		}
	}
}
