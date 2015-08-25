using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class BootManager : SystemBusDevice {
        private ulong[] buffer;

        public BootManager(ulong[] image) : base(1, 1, DeviceType.BootManager) {
            this.buffer = image;
        }

        public override ulong ReadWord(ulong address) {
            return this.buffer[address];
        }

        public override void WriteWord(ulong address, ulong data) {

        }
    }
}