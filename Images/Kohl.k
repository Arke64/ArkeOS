func main() {
	brk;
	RSP = 0x10000;

	R0 = 0d0;

	if (R0) {
		R1 = 0d1;
	} else {
		R1 = 0d2;
	}

	if (R1) {
		R2 = 0d3;
	} else {
		R2 = 0d4;
	}

	brk;

	R0 = 0d5;
	R1 = 0d0;

	while (R0) {
		R1 += 0d1;
		R0 -= 0d1;
	}

	brk;
	dbg (R0, R0, R0);
	cpy (RZERO, RONE, 0d1);
	nop;
	R12 = 1;
	R13 = 2;
	xchg (R12, R13);
	R1 = 1 * (2 + 3) - 9 * 7 / (4 - -3) + +3 ^ (0xC + 3 + (1_1 % 6) / (6 - -(2 - 3)) * 3);
	R2 = 5;
	R3 = 9;
	R4 = R2 + R3;

	R0 = 0;
	R0 += RONE;
	R0 <<<= RONE;

	brk;
	if (R4)
		R5 = 1234;

	R13 = RZERO;
	R14 = RZERO;
	R15 = RONE;

	cas (R13, R14, 0d6);
	cas (R14, R15, 0d8);

	R0 = true;

	if (R0 == R0)
		R1 = 12;

	if (RZERO < RONE) {
		R2 = 34;
		R3 = 45;
	}

	if (RONE & RMAX & true)
		R7 = 13;

	if (RONE & RZERO & false)
		R8 = 13;

	if (RZERO | false) {
		R4 = 56;
		R5 = 78;
		R6 = 90;
	}

	hlt;
}