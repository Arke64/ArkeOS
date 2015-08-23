using System;
using System.IO;
using ArkeOS.Architecture;

namespace ArkeOS.Assembler {
    public static class Program {
        public static void Main(string[] args) {
            Console.Write("Base: ");

            var baseAddressStr = Console.ReadLine();
            var baseAddress = 0UL;

            if (!string.IsNullOrWhiteSpace(baseAddressStr))
                baseAddress = Helpers.ParseLiteral(baseAddressStr);

            Console.Write("Input file: ");

            var input = Console.ReadLine();

            if (!input.EndsWith(".asm"))
                input += ".asm";

            if (!input.Contains("\\"))
                input = @"D:\Code\ArkeOS\Images\" + input;

            var output = Path.ChangeExtension(input, "bin");

            if (File.Exists(input)) {
                File.WriteAllBytes(output, new Assembler(input, baseAddress).Assemble());
            }
            else {
                Console.WriteLine("The specified file cannot be found.");
            }
        }
    }
}