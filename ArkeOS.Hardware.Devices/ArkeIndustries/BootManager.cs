using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
    public class BootManager : SystemBusDevice {
        public ulong[] BootImage { get; set; }

        public BootManager() : base(VendorIds.ArkeIndustries, ArkeIndustries.ProductIds.B100, DeviceType.BootManager) {

        }

        public override ulong ReadWord(ulong address) {
            return this.BootImage[address];
        }

        public override void WriteWord(ulong address, ulong data) {

        }

        public override void Start() {

        }

        public override void Stop() {

        }
    }
}