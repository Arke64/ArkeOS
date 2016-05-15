using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices {
	public interface IProcessor {
		void Start(ulong bootManagerDeviceId);
	}
}
