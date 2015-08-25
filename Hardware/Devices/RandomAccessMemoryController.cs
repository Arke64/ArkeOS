using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class RandomAccessMemoryController : SystemBusDevice {
        private ulong[] memory;

        public ulong Size { get; set; }

        public RandomAccessMemoryController() : base(1, 0, DeviceType.RandomAccessMemory) {

        }

        public override ulong ReadWord(ulong address) {
            return this.memory[address];
        }

        public override void WriteWord(ulong address, ulong data) {
            this.memory[address] = data;
        }

        public override void Reset() {
            this.memory = new ulong[this.Size];
        }

        public override void Stop() {

        }
    }
}