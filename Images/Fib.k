func main() {
	R1 = fib(12);
}

func fib(n) {
	if (n >= 2) return fib(n - 1) + fib(n - 2);
	else return n;
}
