const DeviceIdShift : word = 0d52;
const BusControllerId : word = 0xFFF;
const InterruptDeviceType : word = 0x3;
const KeyboardDeviceType : word = 0x6;
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

struct InterruptDeviceInfo {
    var invalidInstruction : word;
    var divideByZero : word;
    var systemCall : word;
    var systemTimer : word;
    var deviceWaiting : word;
    var cpyComplete : word;
}

var displayBuffer : ptr[word];
var keyboardId : word;

func main() : word {
    var displayId : word = findDevice(DisplayDeviceType);
    var displayHeader : ptr[DisplayDeviceInfo] = displayId << DeviceIdShift;
    displayBuffer = (displayId << DeviceIdShift) + DisplayDataOffset;

    var interruptId : word = findDevice(InterruptDeviceType);
    var interruptHeader : ptr[InterruptDeviceInfo] = interruptId << DeviceIdShift;

    keyboardId = findDevice(KeyboardDeviceType);
    keyboardId <<= DeviceIdShift;

    R2 = displayHeader->columns;
    R3 = displayHeader->rows;
    R4 = displayHeader->width;
    R5 = displayHeader->height;
    R6 = displayHeader->characterWidth;
    R7 = displayHeader->characterHeight;
    R9 = interruptId;
    R10 = keyboardId;

    *(displayBuffer + 0) = 0d72;
    *(displayBuffer + 1) = 0d69;
    *(displayBuffer + 2) = 0d76;
    *(displayBuffer + 3) = 0d76;
    *(displayBuffer + 4) = 0d79;
    *(displayBuffer + 5) = 0d44;
    *(displayBuffer + 6) = 0d32;
    *(displayBuffer + 7) = 0d87;
    *(displayBuffer + 8) = 0d79;
    *(displayBuffer + 9) = 0d82;
    *(displayBuffer + 10) = 0d76;
    *(displayBuffer + 11) = 0d68;
    *(displayBuffer + 12) = 0d33;

    interruptHeader->deviceWaiting = &deviceWaiting;

    inte;

    while (true) {

    }
}

func deviceWaiting() : word {
    brk;

    if (R0 == keyboardId)
        *displayBuffer = *displayBuffer + 1;

    eint;
}

func findDevice(type: word) : word {
    var controllerInfo : ptr[BusControllerInfo] = BusControllerId << DeviceIdShift;
    var deviceCount : word = controllerInfo->deviceCount;
    var device : ptr[BusDeviceInfo] = controllerInfo->deviceList;

    while (deviceCount > 0) {
        if (device->type == type)
            return device->id;

        deviceCount -= 1;
        device += 1;
    }

    return 0;
}
