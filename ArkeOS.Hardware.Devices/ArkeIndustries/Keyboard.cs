using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
    public class Keyboard : SystemBusDevice {
        public Keyboard() : base(ProductIds.Vendor, ProductIds.KB100, DeviceType.Keyboard) { }

        public void TriggerKeyUp(ulong key) => this.RaiseInterrupt(key | (1UL << 63));
        public void TriggerKeyDown(ulong key) => this.RaiseInterrupt(key);
    }
}
