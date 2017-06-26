using System;

namespace ArkeOS.Hardware.Architecture {
    [Flags]
    public enum ParameterFlags {
        None = 0b000,
        Indirect = 0b001,
        RelativeToRIP = 1 << 1,
        RelativeToRSP = 2 << 1,
        RelativeToRBP = 3 << 1,
    }
}
