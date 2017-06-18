using System;

namespace ArkeOS.Hardware.Architecture {
    [Flags]
    public enum ParameterFlags {
        None = 0b00,
        Indirect = 0b01,
        RIPRelative = 0b10,
    }
}
