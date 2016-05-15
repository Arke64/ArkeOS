using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
	public interface IInterruptController {
		int PendingCount { get; }

		void Enqueue(Interrupt type, ulong data1, ulong data2);
		InterruptRecord Dequeue();
		void WaitForInterrupt(int timeout);
	}
}
