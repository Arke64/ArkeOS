namespace ArkeOS.Architecture {
    public enum Interrupt : ulong {
        InvalidInstruction,
        DivideByZero,
        SystemCall,
        SystemTimer,
        DeviceWaiting,
    }
}