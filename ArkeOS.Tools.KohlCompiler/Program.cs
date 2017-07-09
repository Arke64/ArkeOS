using System;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            var c = CompilationOptions.FromArgs(args);
            var e = c.unrecognized;

            foreach (var err in c.unrecognized)
                Console.WriteLine(e);

            foreach (var err in Compiler.Compile(c.result))
                Console.WriteLine(e);
        }
    }
}
