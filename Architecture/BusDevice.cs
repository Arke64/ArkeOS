namespace ArkeOS.Architecture {
    public abstract class BusDevice {
        protected static ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
        protected static ulong MaxId => 0xFFFUL;

        public abstract ulong VendorId { get; }
        public abstract ulong ProductId { get; }

        public ulong this[ulong address] {
            get {
                return this.ReadWord(address);
            }
            set {
                this.WriteWord(address, value);
            }
        }

        public abstract ulong ReadWord(ulong address);
        public abstract void WriteWord(ulong address, ulong data);
    }
}
