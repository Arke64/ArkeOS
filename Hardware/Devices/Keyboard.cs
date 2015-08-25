using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class Keyboard : SystemBusDevice {
        public Keyboard() : base(1, 6, DeviceType.Keyboard) {

        }

        public override ulong ReadWord(ulong address) {
            return 0;
        }

        public override void WriteWord(ulong address, ulong data) {

        }

        public void TriggerKeyUp(ulong key) => this.BusController.RaiseInterrupt(this, key | (1UL << 63));
        public void TriggerKeyDown(ulong key) => this.BusController.RaiseInterrupt(this, key);
    }
}
