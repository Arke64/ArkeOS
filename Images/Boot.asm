DEFINE DeviceAddressBitSize 0d52

DEFINE BootDeviceType 0d5
DEFINE BootDeviceSignature 0x00000000454B5241
DEFINE BootDeviceSignatureOffset 0x0
DEFINE BootDeviceEntryPointOffset 0x1

DEFINE BusDeviceId 0xFFF
DEFINE BusDeviceEntryCountOffset 0d0
DEFINE BusDeviceEntriesOffset 0d1
DEFINE BusDeviceEntryLength 0d4
DEFINE BusDeviceEntryIdOffset 0d0
DEFINE BusDeviceEntryTypeOffset 0d1

SET R0 $BusDeviceId
SL R0 R0 $DeviceAddressBitSize

SET R1 [(R0 + $BusDeviceEntryCountOffset)]

ADD R0 R0 $BusDeviceEntriesOffset

LABEL BootDeviceEnumerationLoopStart
IFZ R1 SET RIP $NoBootDevice
EQ R2 $BootDeviceType [(R0 + $BusDeviceEntryTypeOffset)]
IFNZ R2 SET RIP $TestBootDevice
LABEL BootDeviceTestFailed
SUB R1 R1 RONE
ADD R0 R0 $BusDeviceEntryLength
SET RIP $BootDeviceEnumerationLoopStart

LABEL TestBootDevice
SET R3 [(R0 + $BusDeviceEntryIdOffset)]
SL R3 R3 $DeviceAddressBitSize
EQ R2 $BootDeviceSignature [(R3 + $BootDeviceSignatureOffset)]
IFNZ R2 SET RIP $FoundBootDevice
SET RIP $BootDeviceTestFailed

LABEL FoundBootDevice
SET R0 R3
SET R1 RZERO
SET R2 RZERO
SET R3 RZERO
SET RIP (R0 + $BootDeviceEntryPointOffset)

LABEL NoBootDevice
SET R0 RZERO
SET R1 RZERO
SET R2 RZERO
SET R3 RZERO
HLT
