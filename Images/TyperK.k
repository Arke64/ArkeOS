const DeviceIdShift : word = 0d52;
const BusControllerId : word = 0xFFF;
const InterruptDeviceType : word = 0x3;
const KeyboardDeviceType : word = 0x6;
const DisplayDeviceType : word = 0x7;
const DisplayDataOffset : word = 0x100000;
const BackspaceScanCode : word = 0x0D;
const EnterScanCode : word = 0x28;
const LeftShiftScanCode : word = 0x29;
const RightShiftScanCode : word = 0x34;
const SpaceScanCode : word = 0x38;

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
var columns : word;
var rows : word;
var column : word;
var row : word;
var width : word;
var shifted : bool;

func main() : word {
    var displayId : word = findDevice(DisplayDeviceType);
    var displayHeader : ptr[DisplayDeviceInfo] = displayId << DeviceIdShift;
    displayBuffer = (displayId << DeviceIdShift) + DisplayDataOffset;

    var interruptId : word = findDevice(InterruptDeviceType);
    var interruptHeader : ptr[InterruptDeviceInfo] = interruptId << DeviceIdShift;

    keyboardId = findDevice(KeyboardDeviceType);
    keyboardId <<= DeviceIdShift;

    columns = displayHeader->columns;
    rows = displayHeader->rows;
    column = 0;
    row = 1;
    shifted = false;

    *(displayBuffer + 0) = 0x52;
    *(displayBuffer + 1) = 0x45;
    *(displayBuffer + 2) = 0x41;
    *(displayBuffer + 3) = 0x44;
    *(displayBuffer + 4) = 0x59;

    interruptHeader->deviceWaiting = &deviceWaiting;

    inte;

    while (true) {

    }
}

func deviceWaiting() : word {
    RSP = 0x2_0000;
    RBP = 0x2_0000;

    if (R0 == keyboardId) {
        var code : word = R1 & 0x7FFF_FFFF_FFFF_FFFF;
        var released : bool = (R1 & 0x8000_0000_0000_0000) != 0b0;

        if (code == EnterScanCode & released) {
            column = 0;
            row += 1;
        }
        else if (code == LeftShiftScanCode | code == RightShiftScanCode) {
            if (shifted == true) shifted = false;
            else shifted = true;
        }
        else if (released) {
            var offset : word = 0;

            if (code == BackspaceScanCode) {
                if (column != 0) {
                    column -= 1;
                }
                else {
                    eint;
                }

                offset = row * columns + column;

                code = SpaceScanCode;
            }
            else {
                offset = row * columns + column;

                column += 1;

                if (column > columns) {
                    column = 0;
                    row += 1;
                }
            }

            var char : word = 0x00;
            if (shifted == false) {
                     if (code == 0d00) char = 0x60;
                else if (code == 0d01) char = 0x31;
                else if (code == 0d02) char = 0x32;
                else if (code == 0d03) char = 0x33;
                else if (code == 0d04) char = 0x34;
                else if (code == 0d05) char = 0x35;
                else if (code == 0d06) char = 0x36;
                else if (code == 0d07) char = 0x37;
                else if (code == 0d08) char = 0x38;
                else if (code == 0d09) char = 0x39;
                else if (code == 0d10) char = 0x30;
                else if (code == 0d11) char = 0x2D;
                else if (code == 0d12) char = 0x3D;
                else if (code == 0d13) char = 0x08;
                else if (code == 0d14) char = 0x09;
                else if (code == 0d15) char = 0x71;
                else if (code == 0d16) char = 0x77;
                else if (code == 0d17) char = 0x65;
                else if (code == 0d18) char = 0x72;
                else if (code == 0d19) char = 0x74;
                else if (code == 0d20) char = 0x79;
                else if (code == 0d21) char = 0x75;
                else if (code == 0d22) char = 0x69;
                else if (code == 0d23) char = 0x6F;
                else if (code == 0d24) char = 0x70;
                else if (code == 0d25) char = 0x5B;
                else if (code == 0d26) char = 0x5D;
                else if (code == 0d27) char = 0x5C;
                else if (code == 0d28) char = 0x00;
                else if (code == 0d29) char = 0x61;
                else if (code == 0d30) char = 0x73;
                else if (code == 0d31) char = 0x64;
                else if (code == 0d32) char = 0x66;
                else if (code == 0d33) char = 0x67;
                else if (code == 0d34) char = 0x68;
                else if (code == 0d35) char = 0x6A;
                else if (code == 0d36) char = 0x6B;
                else if (code == 0d37) char = 0x6C;
                else if (code == 0d38) char = 0x3B;
                else if (code == 0d39) char = 0x27;
                else if (code == 0d40) char = 0x0A;
                else if (code == 0d41) char = 0x00;
                else if (code == 0d42) char = 0x7A;
                else if (code == 0d43) char = 0x78;
                else if (code == 0d44) char = 0x63;
                else if (code == 0d45) char = 0x76;
                else if (code == 0d46) char = 0x62;
                else if (code == 0d47) char = 0x6E;
                else if (code == 0d48) char = 0x6D;
                else if (code == 0d49) char = 0x2C;
                else if (code == 0d50) char = 0x2E;
                else if (code == 0d51) char = 0x2F;
                else if (code == 0d52) char = 0x00;
                else if (code == 0d53) char = 0x00;
                else if (code == 0d54) char = 0x00;
                else if (code == 0d55) char = 0x00;
                else if (code == 0d56) char = 0x20;
                else if (code == 0d57) char = 0x00;
                else if (code == 0d58) char = 0x00;
                else if (code == 0d59) char = 0x00;
                else if (code == 0d60) char = 0x00;
                else if (code == 0d61) char = 0x00;
                else if (code == 0d62) char = 0x00;
                else if (code == 0d63) char = 0x00;
                else if (code == 0d64) char = 0x00;
                else if (code == 0d65) char = 0x00;
                else if (code == 0d66) char = 0x00;
                else if (code == 0d67) char = 0x00;
                else if (code == 0d68) char = 0x00;
                else if (code == 0d69) char = 0x00;
                else if (code == 0d70) char = 0x00;
                else if (code == 0d71) char = 0x00;
                else if (code == 0d72) char = 0x00;
                else if (code == 0d73) char = 0x00;
                else if (code == 0d74) char = 0x00;
                else if (code == 0d75) char = 0x00;
                else if (code == 0d76) char = 0x00;
                else if (code == 0d77) char = 0x00;
                else if (code == 0d78) char = 0x00;
                else if (code == 0d79) char = 0x00;
                else if (code == 0d80) char = 0x00;
                else if (code == 0d81) char = 0x00;
                else if (code == 0d82) char = 0x00;
                else if (code == 0d83) char = 0x00;
                else if (code == 0d84) char = 0x00;
                else if (code == 0d85) char = 0x00;
                else if (code == 0d86) char = 0x00;
                else if (code == 0d87) char = 0x2F;
                else if (code == 0d88) char = 0x2A;
                else if (code == 0d89) char = 0x2D;
                else if (code == 0d90) char = 0x2B;
                else if (code == 0d91) char = 0x00;
                else if (code == 0d92) char = 0x2E;
                else if (code == 0d93) char = 0x30;
                else if (code == 0d94) char = 0x31;
                else if (code == 0d95) char = 0x32;
                else if (code == 0d96) char = 0x33;
                else if (code == 0d97) char = 0x34;
                else if (code == 0d98) char = 0x35;
                else if (code == 0d99) char = 0x36;
                else if (code == 0d100) char = 0x37;
                else if (code == 0d101) char = 0x38;
                else if (code == 0d102) char = 0x39;
            }
            else {
                     if (code == 0d00) char = 0x7E;
                else if (code == 0d01) char = 0x21;
                else if (code == 0d02) char = 0x40;
                else if (code == 0d03) char = 0x23;
                else if (code == 0d04) char = 0x24;
                else if (code == 0d05) char = 0x25;
                else if (code == 0d06) char = 0x5E;
                else if (code == 0d07) char = 0x26;
                else if (code == 0d08) char = 0x2A;
                else if (code == 0d09) char = 0x28;
                else if (code == 0d10) char = 0x29;
                else if (code == 0d11) char = 0x5F;
                else if (code == 0d12) char = 0x2B;
                else if (code == 0d13) char = 0x08;
                else if (code == 0d14) char = 0x09;
                else if (code == 0d15) char = 0x51;
                else if (code == 0d16) char = 0x57;
                else if (code == 0d17) char = 0x45;
                else if (code == 0d18) char = 0x52;
                else if (code == 0d19) char = 0x54;
                else if (code == 0d20) char = 0x59;
                else if (code == 0d21) char = 0x55;
                else if (code == 0d22) char = 0x49;
                else if (code == 0d23) char = 0x4F;
                else if (code == 0d24) char = 0x50;
                else if (code == 0d25) char = 0x7B;
                else if (code == 0d26) char = 0x7D;
                else if (code == 0d27) char = 0x7C;
                else if (code == 0d28) char = 0x00;
                else if (code == 0d29) char = 0x41;
                else if (code == 0d30) char = 0x53;
                else if (code == 0d31) char = 0x44;
                else if (code == 0d32) char = 0x46;
                else if (code == 0d33) char = 0x47;
                else if (code == 0d34) char = 0x48;
                else if (code == 0d35) char = 0x4A;
                else if (code == 0d36) char = 0x4B;
                else if (code == 0d37) char = 0x4C;
                else if (code == 0d38) char = 0x3A;
                else if (code == 0d39) char = 0x22;
                else if (code == 0d40) char = 0x0A;
                else if (code == 0d41) char = 0x00;
                else if (code == 0d42) char = 0x5A;
                else if (code == 0d43) char = 0x58;
                else if (code == 0d44) char = 0x43;
                else if (code == 0d45) char = 0x56;
                else if (code == 0d46) char = 0x42;
                else if (code == 0d47) char = 0x4E;
                else if (code == 0d48) char = 0x4D;
                else if (code == 0d49) char = 0x3C;
                else if (code == 0d50) char = 0x3E;
                else if (code == 0d51) char = 0x3F;
                else if (code == 0d52) char = 0x00;
                else if (code == 0d53) char = 0x00;
                else if (code == 0d54) char = 0x00;
                else if (code == 0d55) char = 0x00;
                else if (code == 0d56) char = 0x20;
                else if (code == 0d57) char = 0x00;
                else if (code == 0d58) char = 0x00;
                else if (code == 0d59) char = 0x00;
                else if (code == 0d60) char = 0x00;
                else if (code == 0d61) char = 0x00;
                else if (code == 0d62) char = 0x00;
                else if (code == 0d63) char = 0x00;
                else if (code == 0d64) char = 0x00;
                else if (code == 0d65) char = 0x00;
                else if (code == 0d66) char = 0x00;
                else if (code == 0d67) char = 0x00;
                else if (code == 0d68) char = 0x00;
                else if (code == 0d69) char = 0x00;
                else if (code == 0d70) char = 0x00;
                else if (code == 0d71) char = 0x00;
                else if (code == 0d72) char = 0x00;
                else if (code == 0d73) char = 0x00;
                else if (code == 0d74) char = 0x00;
                else if (code == 0d75) char = 0x00;
                else if (code == 0d76) char = 0x00;
                else if (code == 0d77) char = 0x00;
                else if (code == 0d78) char = 0x00;
                else if (code == 0d79) char = 0x00;
                else if (code == 0d80) char = 0x00;
                else if (code == 0d81) char = 0x00;
                else if (code == 0d82) char = 0x00;
                else if (code == 0d83) char = 0x00;
                else if (code == 0d84) char = 0x00;
                else if (code == 0d85) char = 0x00;
                else if (code == 0d86) char = 0x00;
                else if (code == 0d87) char = 0x2F;
                else if (code == 0d88) char = 0x2A;
                else if (code == 0d89) char = 0x2D;
                else if (code == 0d90) char = 0x2B;
                else if (code == 0d91) char = 0x00;
                else if (code == 0d92) char = 0x2E;
                else if (code == 0d93) char = 0x30;
                else if (code == 0d94) char = 0x31;
                else if (code == 0d95) char = 0x32;
                else if (code == 0d96) char = 0x33;
                else if (code == 0d97) char = 0x34;
                else if (code == 0d98) char = 0x35;
                else if (code == 0d99) char = 0x36;
                else if (code == 0d100) char = 0x37;
                else if (code == 0d101) char = 0x38;
                else if (code == 0d102) char = 0x39;
            }

            if (char != 0x00)
                *(displayBuffer + offset) = char;
        }
    }

    eint;
}

func findDevice(type : word) : word {
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
