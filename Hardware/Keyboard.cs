using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class Keyboard : BusDevice {
        public override ulong VendorId => 1;
        public override ulong ProductId => 5;

        public override ulong ReadWord(ulong address) {
            return 0;
        }

        public override void WriteWord(ulong address, ulong data) {

        }

        public void TriggerKeyUp(ulong key) => this.SystemBus.RaiseInterrupt(this.Id, key | 0x8000000000000000);
        public void TriggerKeyDown(ulong key) => this.SystemBus.RaiseInterrupt(this.Id, key);
    }
}
