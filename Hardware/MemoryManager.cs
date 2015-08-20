using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class MemoryManager : IDevice {
		private ulong[] memory;

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