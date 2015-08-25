using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class BootManager : SystemBusDevice {
        public ulong[] BootImage { get; set; }

        public BootManager() : base(1, 4, DeviceType.BootManager) {

        }

        public override ulong ReadWord(ulong address) {
            return this.BootImage[address];
        }

        public override void WriteWord(ulong address, ulong data) {

        }

        public override void Reset() {

        }

        public override void Stop() {

        }
    }
}