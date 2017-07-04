func main() {
	brk;
	R1 = fib(4);
}

func fib(n) {
	if (n > 2) return fib(n - 1) + fib(n - 2);
	else return n;
}
