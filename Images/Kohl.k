{
	RIP = RONE;
	brk;
	RSP = 0x10000;
	nop;
	R12 = 1;
	R13 = 2;
	xchg R12 R13;
	R1 = 1 * (2 + 3) - 9 * 7 / (4 - -3) + +3 ^ (0xC + 3 + (1_1 % 6) / (6 - -(2 - 3)) * 3);
	R2 = 5;
	R3 = 9;
	R4 = R2 + R3;

	brk;
	if (R4)
		R5 = 1234;

	R13 = RZERO;
	R14 = RZERO;
	R15 = RONE;

	cas R13 R14 0d6;
	cas R14 R15 0d8;

	hlt;
}