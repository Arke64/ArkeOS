const DeviceAddressBitSize = 0d52;

const BootDeviceType = 0d5;
const BootDeviceSignature = 0x00000000454B5241;
const BootDeviceSignatureOffset = 0x0;
const BootDeviceEntryPointOffset = 0x1;

const BusDeviceId = 0xFFF;
const BusDeviceEntryCountOffset = 0d0;
const BusDeviceEntriesOffset = 0d1;
const BusDeviceEntryLength = 0d4;
const BusDeviceEntryIdOffset = 0d0;
const BusDeviceEntryTypeOffset = 0d1;

func main() {
	var busDevice;
	var deviceCount;

	busDevice = BusDeviceId << DeviceAddressBitSize;
	deviceCount = *(busDevice + BusDeviceEntryCountOffset);

	busDevice += BusDeviceEntriesOffset;

	while (deviceCount) {
		if (*(busDevice + BusDeviceEntryTypeOffset) == BootDeviceType) {
			var addr;

			addr = *(busDevice + BusDeviceEntryIdOffset) << DeviceAddressBitSize;

			if (*(addr + BootDeviceSignatureOffset) == BootDeviceSignature) {
				var entry;

				entry = addr + BootDeviceEntryPointOffset;

				R0 = addr;
				RIP = entry;
			}
		}
		else {
			deviceCount -= 1;
			busDevice += BusDeviceEntryLength;
		}
	}
}
