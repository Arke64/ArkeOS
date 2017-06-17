using System;
using System.IO;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            args = new[] { @"..\Images\Kohl.k" };

            if (args.Length < 1) {
                Console.WriteLine("Need at least one argument: the file to assemble");

                return;
            }

            var input = args[0];

            if (!File.Exists(input)) {
                Console.WriteLine("The specified file cannot be found.");

                return;
            }

            Compiler.Compile(input);
        }
    }
}
