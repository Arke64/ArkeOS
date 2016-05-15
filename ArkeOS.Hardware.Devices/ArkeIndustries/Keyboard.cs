using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
    public class Keyboard : SystemBusDevice {
        public Keyboard() : base(VendorIds.ArkeIndustries, ArkeIndustries.ProductIds.KB100, DeviceType.Keyboard) {

        }

        public override ulong ReadWord(ulong address) {
            return 0;
        }

        public override void WriteWord(ulong address, ulong data) {

        }

        public override void Start() {

        }

        public override void Stop() {

        }

        public void TriggerKeyUp(ulong key) => this.BusController.RaiseInterrupt(this, key | (1UL << 63));
        public void TriggerKeyDown(ulong key) => this.BusController.RaiseInterrupt(this, key);
    }
}
