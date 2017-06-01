using ArkeOS.Hardware.Architecture;
using System.IO;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class BootManager : SystemBusDevice {
        private BinaryReader image;
        private bool disposed;

        public BootManager(Stream bootImage) : base(ProductIds.Vendor, ProductIds.MB100, DeviceType.BootManager) {
            this.image = new BinaryReader(bootImage);
            this.disposed = false;
        }

        public override ulong ReadWord(ulong address) {
            this.image.BaseStream.Seek((long)(address * 8), SeekOrigin.Begin);

            return this.image.ReadUInt64();
        }

        protected override void Dispose(bool disposing) {
            if (this.disposed)
                return;

            if (disposing)
                this.image.Dispose();

            this.disposed = true;

            base.Dispose(disposing);
        }
    }
}
