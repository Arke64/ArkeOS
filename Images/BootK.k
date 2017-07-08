const DeviceAddressBitSize : word = 0d52;

const BootDeviceType : word = 0d5;
const BootDeviceSignature : word = 0x00000000454B5241;
const BootDeviceSignatureOffset : word = 0x0;
const BootDeviceEntryPointOffset : word = 0x1;

const BusControllerId : word = 0xFFF;
const BusControllerDeviceCountOffset : word = 0d0;
const BusControllerDeviceListOffset : word = 0d1;

const BusDeviceEntryLength : word = 0d4;
const BusDeviceEntryIdOffset : word = 0d0;
const BusDeviceEntryTypeOffset : word = 0d1;

func main() : word {
	var currentDevice : word* = BusControllerId << DeviceAddressBitSize;
	var deviceCount : word = *(currentDevice + BusControllerDeviceCountOffset);

	currentDevice += BusControllerDeviceListOffset;

	while (deviceCount > 0) {
		if (*(currentDevice + BusDeviceEntryTypeOffset) == BootDeviceType) {
			var deviceAddress : word* = *(currentDevice + BusDeviceEntryIdOffset) << DeviceAddressBitSize;

			if (*(deviceAddress + BootDeviceSignatureOffset) == BootDeviceSignature) {
				RIP = deviceAddress + BootDeviceEntryPointOffset;
			}
		}
		else {
			deviceCount -= 1;
			currentDevice += BusDeviceEntryLength;
		}
	}
}
