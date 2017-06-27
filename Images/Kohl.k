var var1;
var var2;
const c1 = 1232;

func main() {
	var lvar1;
	var lvar2;

	lvar2 = &lvar1;
	lvar1 = c1 + 2;
	*lvar2 *= 2;
	R4 = *lvar2;

	var2 = &var1;
	var1 = 5678;
	R5 = *var2;

	R6 = foo11(1, 0);

	foo22(&var1);
	R7 = var1;

	brk;

	var1 = 0;
	var2 = 0;
	lvar1 = 0;
	lvar2 = 0;

	brk;

	lvar1 = 0d987;
	lvar2 = lvar1;
	R0 = lvar2;

	brk;

	var1 = 0d5678;
	var2 = var1;
	R0 = var2;

	brk;

	if (true) {
		var ivar1;
		var ivar2;

		ivar1 = 0d222;
		ivar2 = ivar1;
		R0 = ivar2;
	}

	brk;

	R1 = foo1(5, 3);
	foo1(1, 1);

	brk;

	R0 = 0d0;
	R1 = 0d0;

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

	brk;
}

func foo1(param1, param2) {
	return foo2(param1 + 2, param2 * 3);
}

func foo2(param1, param2) {
	return 0d1 + param1 + param2;
}

func foo11(param1, param2) {
	param2 = &param1;
	param1 += 3;
	return *param2;
}

func foo22(param1) {
	*param1 += 25;
}