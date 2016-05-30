DEFINE BusDeviceId 0xFFF
DEFINE BusDeviceEntryCountOffset 0d0
DEFINE BusDeviceEntriesOffset 0d1
DEFINE BusDeviceEntryLength 0d4
DEFINE BusDeviceEntryIdOffset 0d0
DEFINE BusDeviceEntryTypeOffset 0d1
DEFINE BusDeviceAddressBitSize 0d52

//R0: type to find and found id
//R1: bus controller cursor
//R2: device count
//R3: if type found
LABEL FindDevice

SET R1 $BusDeviceId
SL R1 R1 $BusDeviceAddressBitSize
SET R2 [(R1 + $BusDeviceEntryCountOffset)]
ADD R1 R1 $BusDeviceEntriesOffset

LABEL FindDeviceLoopStart
IFZ R2 HLT
EQ R3 R0 [(R1 + $BusDeviceEntryTypeOffset)]
IFNZ R3 SET RIP $FoundDevice

SUB R2 R2 RONE
ADD R1 R1 $BusDeviceEntryLength
SET RIP $FindDeviceLoopStart

LABEL FoundDevice
SET R0 [(R1 + $BusDeviceEntryIdOffset)]
SL R0 R0 $BusDeviceAddressBitSize
SET R1 RZERO
SET R2 RZERO
SET R3 RZERO
RET
