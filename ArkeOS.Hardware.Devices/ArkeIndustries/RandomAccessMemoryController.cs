using System;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
    public class RandomAccessMemoryController : SystemBusDevice {
        private ulong[] memory;

        public RandomAccessMemoryController(int size) : base(ProductIds.Vendor, ProductIds.MEM100, DeviceType.RandomAccessMemory) {
			this.memory = new ulong[size];
		}

		public override void Reset() => Array.Clear(this.memory, 0, this.memory.Length);
        public override ulong ReadWord(ulong address) => this.memory[address];
        public override void WriteWord(ulong address, ulong data) => this.memory[address] = data;
		public override void Copy(ulong source, ulong destination, ulong length) => Array.Copy(this.memory, (int)source, this.memory, (int)destination, (int)length);
		public override void Write(ulong destination, ulong[] data) => Array.Copy(data, 0, this.memory, (int)destination, data.Length);

		public override ulong[] Read(ulong source, ulong length) {
			var data = new ulong[length];

			Array.Copy(this.memory, (int)source, data, 0, (int)length);

			return data;
		}

		protected override void Dispose(bool disposing) {
			this.memory = null;

			base.Dispose(disposing);
		}
	}
}
