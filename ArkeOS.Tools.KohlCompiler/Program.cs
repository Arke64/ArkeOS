using System;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            var c = Compiler.FromArgs(args);

            if (c != null) {
                foreach (var e in c.Compile())
                    Console.WriteLine(e);
            }
            else {
                Console.WriteLine("Usage: " + Compiler.Usage);
            }
        }
    }
}
