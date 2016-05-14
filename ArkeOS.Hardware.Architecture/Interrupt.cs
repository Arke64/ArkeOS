namespace ArkeOS.Hardware.Architecture {
    public enum Interrupt : ulong {
        InvalidInstruction,
        DivideByZero,
        SystemCall,
        SystemTimer,
        DeviceWaiting,
    }
}