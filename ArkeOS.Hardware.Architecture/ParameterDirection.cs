using System;

namespace ArkeOS.Hardware.Architecture {
	[Flags]
	public enum ParameterDirection {
		Read = 1,
		Write = 2,
	}
}
