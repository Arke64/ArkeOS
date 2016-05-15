DEFINE DeviceAddressBitSize 0d52
DEFINE BootDeviceType 0d4
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
IFZ R2 MOV $FoundBootDevice RIP
SUB 0d1 R1 R1
ADD $BusDeviceEntryLength R0 R0
MOV $BootDeviceEnumerationLoopStart RIP

LABEL FoundBootDevice
MOV 0d0 R2
MOV [(R0 + $BusDeviceEntryIdOffset)] R0
SL $DeviceAddressBitSize R0 R1
MOV R1 RIP

LABEL NoBootDevice
HLT
MOV $NoBootDevice RIP