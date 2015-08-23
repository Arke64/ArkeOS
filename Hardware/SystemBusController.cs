using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class SystemBusController : BusDevice {
        private BusDevice[] devices;
        private uint nextDevice;

        private ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
        private ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

        public override ulong VendorId => 1;
        public override ulong ProductId => 2;

        public SystemBusController() {
            this.devices = new BusDevice[0xFFF];
            this.nextDevice = 256;

            this.AddDevice(2, new SystemBus());
        }

        public void AddDevice(uint deviceId, BusDevice device) {
            this.devices[deviceId] = device;
        }

        public uint AddDevice(BusDevice device) {
            this.devices[this.nextDevice] = device;

            return this.nextDevice++;
        }

        public void EnumerateBus() {
            var address = (2UL << 52) + 1;
            var count = 0UL;

            foreach (var device in this.devices) {
                if (device == null)
                    continue;

                count++;

                this.WriteWord(address++, device.VendorId);
                this.WriteWord(address++, device.ProductId);
            }

            this.WriteWord(address - count * 2 - 1UL, count);
        }

        public void CopyFrom(ulong[] source, ulong destination, ulong length) {
            for (var i = 0UL; i < length; i++)
                this.WriteWord(destination + i, source[i]);
        }

        public override ulong ReadWord(ulong address) {
            var id = this.GetDeviceId(address);
            address = this.GetAddress(address);

            if (address == BusDevice.MaxAddress) {
                return this.devices[id].VendorId;
            }
            else if (address == BusDevice.MaxAddress - 1) {
                return this.devices[id].ProductId;
            }
            else {
                return this.devices[id].ReadWord(address);
            }
        }

        public override void WriteWord(ulong address, ulong data) {
            var id = this.GetDeviceId(address);
            address = this.GetAddress(address);

            if (address != BusDevice.MaxAddress && address != BusDevice.MaxAddress - 1)
                this.devices[id].WriteWord(address, data);
        }

        private class SystemBus : BusDevice {
            private ulong[] memory;

            public override ulong VendorId => 1;
            public override ulong ProductId => 2;

            public SystemBus() {
                this.memory = new ulong[0xFFF * 2 + 1];
            }

            public override ulong ReadWord(ulong address) => this.memory[address];
            public override void WriteWord(ulong address, ulong data) => this.memory[address] = data;
        }
    }
}