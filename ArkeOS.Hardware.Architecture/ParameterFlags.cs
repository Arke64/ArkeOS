using System;

namespace ArkeOS.Hardware.Architecture {
    [Flags]
    public enum ParameterFlags {
        None = 0b0000,
        Indirect = 0b0001,
        RelativeToRIP = 0b0010,
        RelativeToRSP = 0b0100,
        RelativeToRBP = 0b0110,
        ForbidEmbedded = 0b1000,
    }
}
