﻿using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
	public interface ISystemBusDevice {
		ISystemBusController BusController { get; set; }
		IInterruptController InterruptController { get; set; }

		ulong Id { get; set; }
		ulong VendorId { get; }
		ulong ProductId { get; }
		DeviceType Type { get; }

		void Start();
		void Stop();

		void RaiseInterrupt(ulong data);

		void Copy(ulong source, ulong destination, ulong length);
		ulong[] Read(ulong source, ulong length);
		void Write(ulong destination, ulong[] data);
		ulong ReadWord(ulong address);
		void WriteWord(ulong address, ulong data);
	}
}
