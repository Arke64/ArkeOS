namespace ArkeOS.Architecture {
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