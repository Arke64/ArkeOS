using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class SystemBusController : IWordStream {
        private SystemBusDevice[] devices;
        private uint nextDeviceId;

        private static ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
        private static ulong MaxId => 0xFFFUL;

        private ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
        private ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

        public InterruptController InterruptController { get; set; }

        public SystemBusController() {
            this.devices = new SystemBusDevice[0xFFF];
            this.nextDeviceId = 256;

            this.AddDevice(2, new SystemBusControllerDevice());
        }

        public void RaiseInterrupt(ulong id, ulong data) {
            this.InterruptController.Enqueue((Interrupt)id, data);
        }

        public void AddDevice(uint deviceId, SystemBusDevice device) {
            device.SystemBus = this;
            device.Id = deviceId;

            this.devices[deviceId] = device;
        }

        public uint AddDevice(SystemBusDevice device) {
            this.AddDevice(this.nextDeviceId, device);

            return this.nextDeviceId++;
        }

        public void EnumerateBus() {
            var address = (2UL << 52) + 1;
            var count = 0UL;

            foreach (var device in this.devices) {
                if (device == null)
                    continue;

                count++;

                this.WriteWord(address++, device.DeviceType);
                this.WriteWord(address++, device.VendorId);
                this.WriteWord(address++, device.ProductId);
                this.WriteWord(address++, device.Id);
            }

            this.WriteWord(address - count * 4 - 1UL, count);
        }

        public void CopyFrom(ulong[] source, ulong destination, ulong length) {
            for (var i = 0UL; i < length; i++)
                this.WriteWord(destination + i, source[i]);
        }

        public ulong ReadWord(ulong address) {
            var id = this.GetDeviceId(address);
            address = this.GetAddress(address);

            if (address == SystemBusController.MaxAddress) {
                return this.devices[id].DeviceType;
            }
            else if (address == SystemBusController.MaxAddress - 1) {
                return this.devices[id].VendorId;
            }
            else if (address == SystemBusController.MaxAddress - 2) {
                return this.devices[id].ProductId;
            }
            else if (address == SystemBusController.MaxAddress - 3) {
                return this.devices[id].Id;
            }
            else {
                return this.devices[id].ReadWord(address);
            }
        }

        public void WriteWord(ulong address, ulong data) {
            var id = this.GetDeviceId(address);
            address = this.GetAddress(address);

            if (address < SystemBusController.MaxAddress - 3)
                this.devices[id].WriteWord(address, data);
        }

        private class SystemBusControllerDevice : SystemBusDevice {
            private ulong[] memory;

            public override ulong VendorId => 1;
            public override ulong ProductId => 2;
            public override ulong DeviceType => 2;

            public SystemBusControllerDevice() {
                this.memory = new ulong[0xFFF * 4 + 1];
            }

            public override ulong ReadWord(ulong address) => this.memory[address];
            public override void WriteWord(ulong address, ulong data) => this.memory[address] = data;
        }
    }
}