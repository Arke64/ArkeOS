namespace ArkeOS.Architecture {
    public enum Interrupt : ulong {
        InvalidInstruction = 0x1000,
        DivideByZero,
        SystemCall,
        ProtectionViolation,
        PageNotPresent,
        SystemTimer
    }
}