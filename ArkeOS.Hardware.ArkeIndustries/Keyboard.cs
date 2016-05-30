using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class Keyboard : SystemBusDevice {
        public Keyboard() : base(ProductIds.Vendor, ProductIds.KB100, DeviceType.Keyboard) { }

        public void TriggerKeyUp(ulong scanCode) => this.RaiseInterrupt(scanCode | (1UL << 63));
        public void TriggerKeyDown(ulong scanCode) => this.RaiseInterrupt(scanCode);
    }
}
