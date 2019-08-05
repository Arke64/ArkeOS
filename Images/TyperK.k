const DeviceIdShift : word = 0d52;
const BusControllerId : word = 0xFFF;
const DisplayDeviceType : word = 0x7;
const DisplayDataOffset : word = 0x100000;

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

struct DisplayDeviceInfo {
    var columns : word;
    var rows : word;
    var width : word;
    var height : word;
    var characterWidth : word;
    var characterHeight : word;
}

func main() : word {
    var controllerInfo : ptr[BusControllerInfo] = BusControllerId << DeviceIdShift;
    var deviceCount : word = controllerInfo->deviceCount;
    var device : ptr[BusDeviceInfo] = controllerInfo->deviceList;
    var header : ptr[DisplayDeviceInfo] = 0;
    var base : ptr[word] = 0;

    while (deviceCount > 0) {
        if (device->type == DisplayDeviceType) {
            header = device->id << DeviceIdShift;
            base = (device->id << DeviceIdShift) + DisplayDataOffset;
            deviceCount = 0;
        }
        else {
            deviceCount -= 1;
            device += 1;
        }
    }

    R2 = header->columns;
    R3 = header->rows;
    R4 = header->width;
    R5 = header->height;
    R6 = header->characterWidth;
    R7 = header->characterHeight;

    *(base + 0) = 0d72;
    *(base + 1) = 0d69;
    *(base + 2) = 0d76;
    *(base + 3) = 0d76;
    *(base + 4) = 0d79;
    *(base + 5) = 0d44;
    *(base + 6) = 0d32;
    *(base + 7) = 0d87;
    *(base + 8) = 0d79;
    *(base + 9) = 0d82;
    *(base + 10) = 0d76;
    *(base + 11) = 0d68;
    *(base + 12) = 0d33;
}
