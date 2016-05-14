using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
    public class RandomAccessMemoryController : SystemBusDevice {
        private ulong[] memory;

        public ulong Size { get; set; }

        public RandomAccessMemoryController() : base(Ids.ArkeIndustries.VendorId, Ids.ArkeIndustries.Products.MEM100, DeviceType.RandomAccessMemory) {

        }

        public override ulong ReadWord(ulong address) {
            return this.memory[address];
        }

        public override void WriteWord(ulong address, ulong data) {
            this.memory[address] = data;
        }

        public override void Start() {
            this.memory = new ulong[this.Size];
        }

        public override void Stop() {

        }
    }
}