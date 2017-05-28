namespace ArkeOS.Hardware.Architecture {
    public struct InterruptRecord {
        public Interrupt Type { get; set; }
        public ulong Data1 { get; set; }
        public ulong Data2 { get; set; }
        public ulong Handler { get; set; }
    }
}
