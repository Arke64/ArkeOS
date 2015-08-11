using System;
using System.IO;

namespace ArkeOS.Assembler {
	public static class Program {
		public static void Main(string[] args) {
			Console.Write("Input file: ");

			var input = Console.ReadLine();

			if (!input.EndsWith(".asm"))
				input += ".asm";

			if (!input.Contains("\\"))
				input = @"D:\Code\ArkeOS\Images\" + input;

			var output = Path.ChangeExtension(input, "bin");

			if (File.Exists(input)) {
				File.WriteAllBytes(output, new Assembler(input).Assemble());
			}
			else {
				Console.WriteLine("The specified file cannot be found.");
			}
		}
	}
}