func main() : word {
	R1 = fib(12);
}

func fib(n : word) : word {
	if (n >= 2) return fib(n - 1) + fib(n - 2);
	else return n;
}
