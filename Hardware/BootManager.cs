using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class BootManager : BusDevice {
        private ulong[] buffer;

        public override ulong VendorId => 1;
        public override ulong ProductId => 3;
        public override ulong DeviceType => 4;

        public BootManager(ulong[] image) {
            this.buffer = image;
        }

        public override ulong ReadWord(ulong address) {
            return this.buffer[address];
        }

        public override void WriteWord(ulong address, ulong data) {

        }
    }
}