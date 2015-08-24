using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class MemoryManager : SystemBusDevice {
        private ulong[] memory;

        public override ulong VendorId => 1;
        public override ulong ProductId => 0;
        public override ulong DeviceType => 0;

        public MemoryManager(ulong physicalSize) {
            this.memory = new ulong[physicalSize];
        }

        public override ulong ReadWord(ulong address) {
            return this.memory[address];
        }

        public override void WriteWord(ulong address, ulong data) {
            this.memory[address] = data;
        }
    }
}