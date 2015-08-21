using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class SystemBusController : IDevice {
		private IDevice[] devices;
		private uint nextDevice;

		public SystemBusController() {
			this.devices = new IDevice[0xFFF];
			this.nextDevice = 256;
		}

		public void AddDevice(uint deviceId, IDevice device) {
			this.devices[deviceId] = device;
		}

		public uint AddDevice(IDevice device) {
			this.devices[this.nextDevice] = device;

			return this.nextDevice++;
		}

		public void CopyFrom(ulong[] source, ulong destination, ulong length) {
			for (var i = 0UL; i < length; i++)
				this.WriteWord(destination + i, source[i]);
		}

		public override ulong ReadWord(ulong address) {
			return this.devices[(address & 0xFFF0000000000000UL) >> 52].ReadWord(address & 0x000FFFFFFFFFFFFFUL);
		}

		public override void WriteWord(ulong address, ulong data) {
			this.devices[(address & 0xFFF0000000000000UL) >> 52].WriteWord(address & 0x000FFFFFFFFFFFFFUL, data);
		}
	}
}