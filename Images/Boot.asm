DEFINE DeviceAddressBitSize 0d52

DEFINE BootDeviceType 0d5
DEFINE BootDeviceSignature 0x000000444556494E
DEFINE BootDeviceSignatureOffset 0x0
DEFINE BootDeviceEntryPointOffset 0x1

DEFINE BusDeviceId 0xFFF
DEFINE BusDeviceEntryCountOffset 0d0
DEFINE BusDeviceEntriesOffset 0d1
DEFINE BusDeviceEntryLength 0d4
DEFINE BusDeviceEntryIdOffset 0d0
DEFINE BusDeviceEntryTypeOffset 0d1

MOV $BusDeviceId R0
SL $DeviceAddressBitSize R0 R0

MOV [(R0 + $BusDeviceEntryCountOffset)] R1

ADD $BusDeviceEntriesOffset R0 R0

LABEL BootDeviceEnumerationLoopStart
IFZ R1 MOV $NoBootDevice RIP
EQ $BootDeviceType [(R0 + $BusDeviceEntryTypeOffset)] R2
IFNZ R2 MOV $TestBootDevice RIP
LABEL BootDeviceTestFailed
SUB 0d1 R1 R1
ADD $BusDeviceEntryLength R0 R0
MOV $BootDeviceEnumerationLoopStart RIP

LABEL TestBootDevice
MOV [(R0 + $BusDeviceEntryIdOffset)] R3
SL $DeviceAddressBitSize R3 R3
EQ $BootDeviceSignature [(R3 + $BootDeviceSignatureOffset)] R2
IFNZ R2 MOV $FoundBootDevice RIP
MOV $BootDeviceTestFailed RIP

LABEL FoundBootDevice
MOV R3 R0
MOV 0d0 R1
MOV 0d0 R2
MOV 0d0 R3
MOV (R0 + $BootDeviceEntryPointOffset) RIP

LABEL NoBootDevice
HLT
MOV $NoBootDevice RIP