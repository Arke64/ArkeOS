using ArkeOS.Hardware.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class SystemBusController : ISystemBusController {
        private ISystemBusDevice[] devices;
        private ulong nextDeviceId;
        private bool disposed;

        private static ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
        private static ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

        public IProcessor Processor { get; set; }
        public IInterruptController InterruptController { get; set; }

        public IReadOnlyList<ISystemBusDevice> Devices => this.devices.Where(d => d != null).ToList();

        public int AddressBits => 52;
        public ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
        public ulong MaxId => 0xFFFUL;

        public SystemBusController() {
            this.devices = new ISystemBusDevice[this.MaxId + 1];
            this.devices[this.MaxId] = new SystemBusControllerDevice();
            this.nextDeviceId = 0;
            this.disposed = false;
        }

        public void Reset() {
            var count = (ulong)this.Devices.Count();
            var memory = new ulong[count * 4 + 1];
            var index = 0;
            var bootId = 0UL;

            memory[index++] = count - 1;

            foreach (var device in this.Devices) {
                memory[index++] = device.Id;
                memory[index++] = (ulong)device.Type;
                memory[index++] = device.VendorId;
                memory[index++] = device.ProductId;

                if (device.Type == DeviceType.BootManager)
                    bootId = device.Id;
            }

            ((SystemBusControllerDevice)this.devices[this.MaxId]).SetDeviceMap(memory);

            this.Processor.StartAddress = bootId << this.AddressBits;

            foreach (var d in this.Devices)
                d.Reset();
        }

        protected virtual void Dispose(bool disposing) {
            if (this.disposed)
                return;

            if (disposing) {
                foreach (var d in this.Devices)
                    d.Dispose();

                this.devices = null;
            }

            this.disposed = true;
        }

        public void Dispose() {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public ulong AddDevice(ISystemBusDevice device) {
            if (this.nextDeviceId > this.MaxId - 1) throw new InvalidOperationException("Max devices exceeded.");

            device.Id = this.nextDeviceId;
            device.BusController = this;
            device.InterruptController = this.InterruptController;

            this.devices[this.nextDeviceId] = device;

            return this.nextDeviceId++;
        }

        public void RemoveDevice(ISystemBusDevice device) {
            for (var i = 0UL; i < this.MaxId; i++) {
                if (this.devices[i] == device) {
                    this.devices[i] = null;

                    device.Dispose();
                }
            }
        }

        public ulong[] Read(ulong source, ulong length) => this.devices[SystemBusController.GetDeviceId(source)].Read(SystemBusController.GetAddress(source), length);
        public void Write(ulong destination, ulong[] data) => this.devices[SystemBusController.GetDeviceId(destination)].Write(SystemBusController.GetAddress(destination), data);
        public ulong ReadWord(ulong address) => this.devices[SystemBusController.GetDeviceId(address)].ReadWord(SystemBusController.GetAddress(address));
        public void WriteWord(ulong address, ulong data) => this.devices[SystemBusController.GetDeviceId(address)].WriteWord(SystemBusController.GetAddress(address), data);

        public void Copy(ulong source, ulong destination, ulong length) {
            var sourceDevice = this.devices[SystemBusController.GetDeviceId(source)];
            var destinationDevice = this.devices[SystemBusController.GetDeviceId(destination)];

            if (sourceDevice.Id == destinationDevice.Id) {
                sourceDevice.Copy(SystemBusController.GetAddress(source), SystemBusController.GetAddress(destination), length);
            }
            else {
                destinationDevice.Write(SystemBusController.GetAddress(destination), sourceDevice.Read(SystemBusController.GetAddress(source), length));
            }
        }

        private class SystemBusControllerDevice : SystemBusDevice {
            private ulong[] memory;

            public SystemBusControllerDevice() : base(ProductIds.Vendor, ProductIds.BC100, DeviceType.SystemBusController) { }

            public void SetDeviceMap(ulong[] memory) => this.memory = memory;

            public override ulong ReadWord(ulong address) => this.memory[address];

            protected override void Dispose(bool disposing) {
                this.memory = null;

                base.Dispose(disposing);
            }
        }
    }
}
