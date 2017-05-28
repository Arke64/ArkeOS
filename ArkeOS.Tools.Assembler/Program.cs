using System;
using System.IO;

namespace ArkeOS.Tools.Assembler {
    public static class Program {
        public static void Main(string[] args) {
            var input = args[0];

            if (!File.Exists(input)) {
                Console.WriteLine("The specified file cannot be found.");

                return;
            }

            File.WriteAllBytes(Path.ChangeExtension(input, "bin"), new Assembler().Assemble(File.ReadAllLines(input)));
        }
    }
}
