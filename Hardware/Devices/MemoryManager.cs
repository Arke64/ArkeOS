using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class MemoryManager : SystemBusDevice {
        private ulong[] memory;

        public MemoryManager(ulong physicalSize) : base(1, 0, DeviceType.RandomAccessMemory) {
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