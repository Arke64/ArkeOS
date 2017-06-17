namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            args = new[] { @"..\Images\Kohl.k" };

            var compiler = new Compiler { OutputName = @"..\Images\Kohl.bin" };

            foreach (var a in args)
                compiler.AddSource(a);

            compiler.Compile();
        }
    }
}
