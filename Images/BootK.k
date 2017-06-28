const DeviceAddressBitSize = 0d52;

const BootDeviceType = 0d5;
const BootDeviceSignature = 0x00000000454B5241;
const BootDeviceSignatureOffset = 0x0;
const BootDeviceEntryPointOffset = 0x1;

const BusControllerId = 0xFFF;
const BusControllerDeviceCountOffset = 0d0;
const BusControllerDeviceListOffset = 0d1;

const BusDeviceEntryLength = 0d4;
const BusDeviceEntryIdOffset = 0d0;
const BusDeviceEntryTypeOffset = 0d1;

func main() {
	var currentDevice = BusControllerId << DeviceAddressBitSize;
	var deviceCount = *(currentDevice + BusControllerDeviceCountOffset);

	currentDevice += BusControllerDeviceListOffset;

	while (deviceCount) {
		if (*(currentDevice + BusDeviceEntryTypeOffset) == BootDeviceType) {
			var deviceAddress = *(currentDevice + BusDeviceEntryIdOffset) << DeviceAddressBitSize;

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
