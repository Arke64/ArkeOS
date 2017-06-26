ArkeOS
======
ArkeOS is an entirely new computer architecture.

Registers
=========
- R0-R15: General purpose use
- RI0-RI7: General purpose use for interrupts only
- RZERO: 0x0 register
- RONE: 0x1 register
- RMAX: 0xFF.. register
- RBP: Current base address used for calculating relative to offsets
- RSP: Address of the current stack position
- RIP: Address of the current instruction
- RSIP: Address of the instruction executing when an interrupt fires
- RINT1: Data made available from an interrupt
- RINT2: Data made available from an interrupt

Configuration
=============
- 0x00: System tick interval
- 0x01: Instruction cache size

Interrupts
==========
- 0x00: Invalid instruction (Code, Address)
- 0x01: Divide by zero (Address)
- 0x02: System call
- 0x03: System timer
- 0x04: Device waiting (Device, Data)
- 0x05: CPY complete (Source, Destination)
- 0x06-0xFF: Reserved
- 0x100-0xFFF: Undefined

Instruction Format
==================
Each instruction begins with one word. Calculated and literal values are embedded recursively as they appear. There is no limit on how far calculated parameters can recurse.

Base
----
- 8b Code
- 30b Parameters (3x10b parameter info)
- 12b Conditional (1b is enabled, 1b when non-zero, 10b parameter info)
- 14b Reserved

Calculated Parameter
--------------------
- 44b Parameters (4x10b: 1b is positive, 10b parameter info; format of [base + index * scale + offset])
- 20b Reserved

Parameter Info
--------------
- 1b is indirect
- 2b relative (0 = none, 1 = RIP, 2 = RSP, 3 = R0; before indirection)
- 2b type (0 = calculated, 1 = register, 2 = literal, 3 = stack)
- 5b register

Instructions
============

Basic
-----
- 00 HLT: Stops the processor
- 01 NOP: No operation
- 02 INT A B C: Raise the interrupt specified by A passing B to RINT1 and C to RINT2
- 03 EINT: Return from the interrupt
- 04 INTE: Enables interrupts
- 05 INTD: Disables interrupts
- 06 XCHG A B: Swap A and B
- 07 CAS A B C: If A and B are equal, set A to C, else set B to A
- 08 SET A B: Set A to B
- 09 CPY A B C: Copy C bytes from B into A
- 10 CALL A: Push RIP onto the stack and set RIP to A
- 11 RET: Pop the stack into RIP

Math
----
- 20 ADD A B C: Set A to B + C
- 21 ADDF A B C: Set A to B + C interpreting each as a double precision IEEE 754 value
- 22 SUB A B C: Set A to B - C
- 23 SUBF A B C: Set A to B - C interpreting each as a double precision IEEE 754 value
- 24 DIV A B C: Set A to B / C
- 25 DIVF A B C: Set A to B / C interpreting each as a double precision IEEE 754 value
- 26 MUL A B C: Set A to B * C
- 27 MULF A B C: Set A to B * C interpreting each as a double precision IEEE 754 value
- 28 POW A B C: Set A to B ^ C
- 29 POWF A B C: Set A to B ^ C interpreting each as a double precision IEEE 754 value
- 30 MOD A B C: Set A to B % C
- 31 MODF A B C: Set A to B % C interpreting each as a double precision IEEE 754 value
- 32 ITOF A B: Convert the integer B to a double precision IEEE 754 value and store it in A
- 33 FTOI A B: Convert the IEEE 754 value in B to a double precision integer and store it in A

Logic
-----
- 40 SR A B C: Set A to B >> C
- 41 SL A B C: Set A to B << C
- 42 RR A B C: Set A to B >> C rotating in the removed bits
- 43 RL A B C: Set A to B << C rotating in the removed bits
- 44 NAND A B C: Set A to !(B & C)
- 45 AND A B C: Set A to B & C
- 46 NOR A B C: Set A to !(B | C)
- 47 OR A B C: Set A to B | C
- 48 NXOR A B C: Set A to !(B ^ C)
- 49 XOR A B C: Set A to B ^ C
- 50 NOT A B: Set A to !B
- 51 GT A B C: Set A to B > C
- 52 GTE A B C: Set A to B >= C
- 53 LT A B C: Set A to B < C
- 54 LTE A B C: Set A to B <= C
- 55 EQ A B C: Set A to B == C
- 56 NEQ A B C: Set A to B != C

Debug
-----
- 60 DBG A B C: Debug use
- 61 BRK: Break execution

Devices
=======

Interrupt Controller
--------------------
Each address stores the address of the the interrupt handler for the respective vector. For example, address 2 contains the address of the handler for interrupt 2. Enqueuing and dequeueing interrupts occurs outside the system bus.

Keyboard
--------
- 0x00: `
- 0x01: 1
- 0x02: 2
- 0x03: 3
- 0x04: 4
- 0x05: 5
- 0x06: 6
- 0x07: 7
- 0x08: 8
- 0x09: 9
- 0x0A: 0
- 0x0B: -
- 0x0C: =
- 0x0D: BACKSPACE
- 0x0E: TAB
- 0x0F: Q
- 0x10: W
- 0x11: E
- 0x12: R
- 0x13: T
- 0x14: Y
- 0x15: U
- 0x16: I
- 0x17: O
- 0x18: P
- 0x19: [
- 0x1A: ]
- 0x1B: \
- 0x1C: CAPS LOCK
- 0x1D: A
- 0x1E: S
- 0x1F: D
- 0x20: F
- 0x21: G
- 0x22: H
- 0x23: J
- 0x24: K
- 0x25: L
- 0x26: ;
- 0x27: '
- 0x28: ENTER
- 0x29: LEFT SHIFT
- 0x2A: Z
- 0x2B: X
- 0x2C: C
- 0x2D: V
- 0x2E: B
- 0x2F: N
- 0x30: M
- 0x31: ,
- 0x32: .
- 0x33: /
- 0x34: RIGHT SHIFT
- 0x35: LEFT CONTROL
- 0x36: OPTION
- 0x37: LEFT ALT
- 0x38: SPACE
- 0x39: RIGHT ALT
- 0x3A: MENU
- 0x3B: RIGHT CONTROL
- 0x3C: ESCAPE
- 0x3D: F1
- 0x3E: F2
- 0x3F: F3
- 0x40: F4
- 0x41: F5
- 0x42: F6
- 0x43: F7
- 0x44: F8
- 0x45: F9
- 0x46: F10
- 0x47: F11
- 0x48: F12
- 0x49: PRINT SCREEN
- 0x4A: SCROLL LOCK
- 0x4B: PAUSE BREAK
- 0x4C: INSERT
- 0x4D: DELETE
- 0x4E: HOME
- 0x4F: END
- 0x50: PAGE UP
- 0x51: PAGE DOWN
- 0x52: UP
- 0x53: DOWN
- 0x54: LEFT
- 0x55: RIGHT
- 0x56: NUM LOCK
- 0x57: NUMERIC /
- 0x58: NUMERIC *
- 0x59: NUMERIC -
- 0x5A: NUMERIC +
- 0x5B: NUMERIC ENTER
- 0x5C: NUMERIC .
- 0x5D: NUMERIC 0
- 0x5E: NUMERIC 1
- 0x5F: NUMERIC 2
- 0x60: NUMERIC 3
- 0x61: NUMERIC 4
- 0x62: NUMERIC 5
- 0x63: NUMERIC 6
- 0x64: NUMERIC 7
- 0x65: NUMERIC 8
- 0x66: NUMERIC 9
