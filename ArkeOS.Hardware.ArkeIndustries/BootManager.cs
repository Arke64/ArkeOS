using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class BootManager : SystemBusDevice {
		private ulong[] image;

        public BootManager(ulong[] bootImage) : base(ProductIds.Vendor, ProductIds.MB100, DeviceType.BootManager) {
			this.image = bootImage;
		}

        public override ulong ReadWord(ulong address) => this.image[address];
    }
}