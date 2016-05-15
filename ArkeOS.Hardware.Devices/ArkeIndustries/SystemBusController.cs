using System;
using System.Collections.Generic;
using System.Linq;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
	public class SystemBusController : ISystemBusController {
		private ISystemBusDevice[] devices;
		private ulong nextDeviceId;

		private ulong GetDeviceId(ulong address) => (address & 0xFFF0000000000000UL) >> 52;
		private ulong GetAddress(ulong address) => address & 0x000FFFFFFFFFFFFFUL;

		public IProcessor Processor { get; set; }
		public IInterruptController InterruptController { get; set; }

		public IReadOnlyList<ISystemBusDevice> Devices => this.devices;

		public int AddressBits => 52;
		public ulong MaxAddress => 0x000FFFFFFFFFFFFFUL;
		public ulong MaxId => 0xFFFUL;

		public SystemBusController() {
			this.devices = new ISystemBusDevice[this.MaxId + 1];
			this.nextDeviceId = 0;
		}

		public void Start() {
			var devices = this.devices.Where(d => d != null);
			var count = (ulong)devices.Count();
			var memory = new ulong[count * 4 + 1];
			var index = 0;
			var bootId = 0UL;

			memory[index++] = count;

			foreach (var device in devices) {
				memory[index++] = device.Id;
				memory[index++] = (ulong)device.Type;
				memory[index++] = device.VendorId;
				memory[index++] = device.ProductId;

				if (device.Type == DeviceType.BootManager)
					bootId = device.Id;
			}

			this.devices[this.MaxId] = new SystemBusControllerDevice(memory);

			this.Processor.Start(bootId);

			foreach (var d in devices)
				if (d != this.Processor)
					d.Start();
		}

		public void Stop() {

		}

		public ulong AddDevice(ISystemBusDevice device) {
			if (this.nextDeviceId > this.MaxId - 1) throw new InvalidOperationException("Max devices exceeded.");

			device.Id = this.nextDeviceId;
			device.BusController = this;
			device.InterruptController = this.InterruptController;

			this.devices[this.nextDeviceId] = device;

			return this.nextDeviceId++;
		}

		public ulong ReadWord(ulong address) => this.devices[this.GetDeviceId(address)].ReadWord(this.GetAddress(address));
		public void WriteWord(ulong address, ulong data) => this.devices[this.GetDeviceId(address)].WriteWord(this.GetAddress(address), data);

		private class SystemBusControllerDevice : SystemBusDevice {
			private ulong[] memory;

			public SystemBusControllerDevice(ulong[] memory) : base(ProductIds.Vendor, ProductIds.BC100, DeviceType.SystemBusController) {
				this.memory = memory;
			}

			public override ulong ReadWord(ulong address) => this.memory[address];

			public override void Stop() {
				Array.Clear(this.memory, 0, this.memory.Length);
			}
		}
	}
}
