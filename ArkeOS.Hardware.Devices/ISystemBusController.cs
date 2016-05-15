using System.Collections.Generic;
using ArkeOS.Utilities;

namespace ArkeOS.Hardware.Devices {
	public interface ISystemBusController : IWordStream {
		IProcessor Processor { get; set; }
		IInterruptController InterruptController { get; set; }

		IReadOnlyList<ISystemBusDevice> Devices { get; }

		int AddressBits { get; }
		ulong MaxAddress { get; }
		ulong MaxId { get; }

		void Start();
		void Stop();
		ulong AddDevice(ISystemBusDevice device);
	}
}
