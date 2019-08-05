const DeviceIdShift : word = 0d52;
const BusControllerId : word = 0xFFF;
const BootDeviceType : word = 0d5;
const BootDeviceSignature : word = 0x00000000454B5241;

struct BusControllerInfo {
    var deviceCount : word;
    var deviceList : ptr[BusDeviceInfo];
}

struct BusDeviceInfo {
    var id : word;
    var type : word;
    var vendorId : word;
    var productId : word;
}

struct BootDeviceHeader {
    var signature : word;
    var entryPoint : word;
}

func main() : word {
    R1 = 6;

    return 0;

    var controllerInfo : ptr[BusControllerInfo] = BusControllerId << DeviceIdShift;
    var deviceCount : word = controllerInfo->deviceCount;
    var device : ptr[BusDeviceInfo] = controllerInfo->deviceList;

    while (deviceCount > 0) {
        if (device->type == BootDeviceType) {
            var header : ptr[BootDeviceHeader] = device->id << DeviceIdShift;

            if (header->signature == BootDeviceSignature)
                RIP = &header->entryPoint;
        }

        deviceCount -= 1;
        device += 1;
    }
}
