using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
    public abstract class SystemBusDevice {
        public SystemBusController BusController { get; set; }
        public ulong Id { get; set; }

        public ulong VendorId { get; }
        public ulong ProductId { get; }
        public DeviceType Type { get; }

        protected SystemBusDevice(ulong vendorId, ulong productId, DeviceType type) {
            this.VendorId = vendorId;
            this.ProductId = productId;
            this.Type = type;
        }

        public void Copy(ulong source, ulong destination, ulong length) {
            this.Write(destination, this.Read(source, length));
        }

        public virtual ulong[] Read(ulong source, ulong length) {
            var buffer = new ulong[length];

            for (var i = 0UL; i < length; i++)
                buffer[i] = this.ReadWord(source + i);

            return buffer;
        }

        public virtual void Write(ulong destination, ulong[] data) {
            for (var i = 0UL; i < (ulong)data.Length; i++)
                this.WriteWord(destination + i, data[i]);
        }

        public abstract ulong ReadWord(ulong address);
        public abstract void WriteWord(ulong address, ulong data);
        public abstract void Start();
        public abstract void Stop();
    }
}
