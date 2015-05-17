namespace ArkeOS.Executable {
	public enum Interrupt {
		InvalidInstruction,
		DivideByZero,
		SystemCall,
		ProtectionViolation,
		PageNotPresent,
		SystemTimer,
		DeviceWaiting
	}
}
