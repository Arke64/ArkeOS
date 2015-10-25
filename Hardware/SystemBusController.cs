using System;
using System.Linq;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class SystemBusController : IWordStream {
        private SystemBusDevice[] devices;
        private ulong nextDeviceId;

        private ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
        private ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

        public InterruptController InterruptController { get; set; }

        public static ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
        public static ulong MaxId => 0xFFFUL;

        public SystemBusController() {
            this.devices = new SystemBusDevice[SystemBusController.MaxId];
            this.nextDeviceId = 128;

            this.AddDevice(Architecture.Ids.Devices.SystemBusController, new SystemBusControllerDevice());
        }

        public void RaiseInterrupt(SystemBusDevice device, ulong data2) {
            this.InterruptController?.Enqueue(Interrupt.DeviceWaiting, device.Id, data2);
        }

        public void AddDevice(ulong deviceId, SystemBusDevice device) {
            device.Id = deviceId;
            device.BusController = this;

            this.devices[deviceId] = device;
        }

        public ulong AddDevice(SystemBusDevice device) {
            this.AddDevice(this.nextDeviceId, device);

            return this.nextDeviceId++;
        }

        public void Start() {
            var address = (Ids.Devices.SystemBusController << 52) + 1;
            var count = 0UL;

            foreach (var device in this.devices) {
                if (device == null)
                    continue;

                count++;

                this.WriteWord(address++, (ulong)device.Type);
                this.WriteWord(address++, device.VendorId);
                this.WriteWord(address++, device.ProductId);
                this.WriteWord(address++, device.Id);
            }

            this.WriteWord(address - count * 4 - 1UL, count);

            foreach (var d in this.devices.Where(d => d?.Type != DeviceType.Processor))
                d?.Start();

            foreach (var d in this.devices.Where(d => d?.Type == DeviceType.Processor))
                d?.Start();
        }

        public void Stop() {
            foreach (var d in this.devices)
                d?.Stop();
        }

        public ulong ReadWord(ulong address) {
            var id = this.GetDeviceId(address);
            address = this.GetAddress(address);

            if (address < SystemBusController.MaxAddress - 3) {
                return this.devices[id].ReadWord(address);
            }
            else if (address == SystemBusController.MaxAddress) {
                return (ulong)this.devices[id].Type;
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
                return 0;
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

            public SystemBusControllerDevice() : base(Ids.ArkeIndustries.VendorId, Ids.ArkeIndustries.Products.AB100, DeviceType.SystemBusController) {
                this.memory = new ulong[SystemBusController.MaxId * 4 + 1];
            }

            public override ulong ReadWord(ulong address) => this.memory[address];
            public override void WriteWord(ulong address, ulong data) => this.memory[address] = data;

            public override void Start() {

            }

            public override void Stop() {
                Array.Clear(this.memory, 0, this.memory.Length);
            }
        }
    }
}