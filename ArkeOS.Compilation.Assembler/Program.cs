using System;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.Assembler {
	public static class Program {
		public static void Main(string[] args) {
			var input = args[0];

			if (!File.Exists(input)) {
				Console.WriteLine("The specified file cannot be found.");

				return;
			}

			var configs = args.Skip(1).ToList();
			var config = configs.Select(c => c.Split('=')).ToDictionary(c => c[0].Substring(1), c => c?[1]);
			var assembler = new Assembler();

			File.WriteAllBytes(Path.ChangeExtension(input, "bin"), assembler.Assemble(File.ReadAllLines(input)));
		}
	}
}