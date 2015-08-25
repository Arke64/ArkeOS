﻿using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class SystemBusController : IWordStream {
        private SystemBusDevice[] devices;
        private ulong nextDeviceId;

        public static ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
        public static ulong MaxId => 0xFFFUL;

        private ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
        private ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

        public InterruptController InterruptController { get; set; }

        public static ulong RandomAccessMemoryDeviceId => (ulong)DeviceType.RandomAccessMemory;
        public static ulong ProcessorControllerDeviceId => (ulong)DeviceType.ProcessorController;
        public static ulong SystemBusControllerDeviceId => (ulong)DeviceType.SystemBusController;
        public static ulong BootManagerDeviceId => (ulong)DeviceType.BootManager;
        public static ulong InterruptControllerDeviceId => (ulong)DeviceType.InterruptController;

        public SystemBusController() {
            this.devices = new SystemBusDevice[SystemBusController.MaxId];
            this.nextDeviceId = 16;

            this.AddDevice(SystemBusController.SystemBusControllerDeviceId, new SystemBusControllerDevice());
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

        public void EnumerateBus() {
            var address = (2UL << 52) + 1;
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
        }

        public void CopyFrom(ulong[] source, ulong destination, ulong length) {
            for (var i = 0UL; i < length; i++)
                this.WriteWord(destination + i, source[i]);
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

            public SystemBusControllerDevice() : base(1, 2, DeviceType.SystemBusController) {
                this.memory = new ulong[SystemBusController.MaxId * 4 + 1];
            }

            public override ulong ReadWord(ulong address) => this.memory[address];
            public override void WriteWord(ulong address, ulong data) => this.memory[address] = data;
        }
    }
}