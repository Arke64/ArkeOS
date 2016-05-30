DEFINE InterruptControllerDeviceType 0d3
DEFINE KeyboardDeviceType 0d6
DEFINE DisplayDeviceType 0d7

DEFINE InterruptControllerDeviceWaitingOffset 0x4

DEFINE DisplayColumnsOffset 0d0
DEFINE DisplayRowsOffset 0d1
DEFINE DisplayWidthOffset 0d2
DEFINE DisplayHeightOffset 0d3
DEFINE DisplayCharacterWidthOffset 0d4
DEFINE DisplayCharacterHeightOffset 0d5
DEFINE DisplayCharacterOffset 0x100000
DEFINE DisplayBackspaceCharacter 0x08
DEFINE DisplayNewLineCharacter 0x0D
DEFINE DisplaySpaceCharacter 0x20

CONST 0x0000004E49564544

CPY RZERO $SOF ($EOF + -$SOF)
SET RIP RZERO

LABEL SOF

SET RSP 0x10000

SET R0 $InterruptControllerDeviceType
CALL $FindDevice

SET [(R0 + $InterruptControllerDeviceWaitingOffset)] $DeviceWaiting

SET R0 $KeyboardDeviceType
CALL $FindDevice
SET R5 R0

SET R0 $DisplayDeviceType
CALL $FindDevice

SET R6 [(R0 + $DisplayRowsOffset)]
SET R7 [(R0 + $DisplayColumnsOffset)]
SET R8 [(R0 + $DisplayHeightOffset)]
SET R9 [(R0 + $DisplayWidthOffset)]
SET R10 [(R0 + $DisplayCharacterHeightOffset)]
SET R11 [(R0 + $DisplayCharacterWidthOffset)]

ADD R0 R0 $DisplayCharacterOffset
SET R1 RZERO
SET R2 RZERO
INTE
HLT

LABEL DeviceWaiting
SL RI0 RONE 0d63
AND RI0 RI0 RINT2
IFNZ RI0 EINT
EQ RI0 RINT1 R5
IFZ RI0 HLT

EQ RI0 RINT2 $DisplayBackspaceCharacter
IFZ RI0 SET RIP $CheckNewLine
IFZ R2 EINT
SUB R2 R2 RONE
SET [(R0 + R1 * R7 + R2)] $DisplaySpaceCharacter
EINT

LABEL CheckNewLine
EQ RI0 RINT2 $DisplayNewLineCharacter
IFZ RI0 SET RIP $PrintCharacter
SET R2 RZERO
ADD R1 R1 RONE
SET RIP $CheckScroll

LABEL PrintCharacter
SET [(R0 + R1 * R7 + R2)] RINT2

ADD R2 R2 RONE
GTE RI0 R2 R7
IFZ RI0 EINT
SET R2 RZERO
ADD R1 R1 RONE

LABEL CheckScroll
GTE RI0 R1 R6
IFZ RI0 EINT

SUB R1 R1 RONE
CPY R0 (R0 + R7) (RZERO + R6 * R7 + -R7)

SET RI0 R7
LABEL ScrollClearLoopStart
SUB RI0 RI0 RONE
SET [(R0 + R1 * R7 + RI0)] $DisplaySpaceCharacter
IFZ RI0 EINT
SET RIP $ScrollClearLoopStart

EINT

INCLUDE .\Images\FindDevice.asm

LABEL EOF