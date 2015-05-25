namespace ArkeOS.ISA {
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