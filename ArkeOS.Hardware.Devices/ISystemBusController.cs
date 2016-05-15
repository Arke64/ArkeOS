using System;
using System.Collections.Generic;
using ArkeOS.Utilities;

namespace ArkeOS.Hardware.Devices {
	public interface ISystemBusController : IWordStream, IDisposable {
		IProcessor Processor { get; set; }
		IInterruptController InterruptController { get; set; }

		IReadOnlyList<ISystemBusDevice> Devices { get; }

		int AddressBits { get; }
		ulong MaxAddress { get; }
		ulong MaxId { get; }

		void Reset();

		ulong AddDevice(ISystemBusDevice device);

		void Copy(ulong source, ulong destination, ulong length);
		ulong[] Read(ulong source, ulong length);
		void Write(ulong destination, ulong[] data);
	}
}
