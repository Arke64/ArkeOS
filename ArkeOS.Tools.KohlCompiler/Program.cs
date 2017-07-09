using System;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            var c = CompilationOptions.FromArgs(args);

            foreach (var err in c.unrecognized)
                Console.WriteLine(err);

            foreach (var err in Compiler.Compile(c.result))
                Console.WriteLine(err);
        }
    }
}
