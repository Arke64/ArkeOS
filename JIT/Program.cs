using System;
using System.IO;

namespace ArkeOS.Interpreter {
	public class Program {
		private static bool CheckArgs(string[] args, out string path) {
			path = string.Empty;

			if (args.Length != 1) {
				Console.WriteLine("Usage: JIT.exe program");

				return false;
			}

			path = args[0];

			if (!File.Exists(path)) {
				Console.WriteLine("The specified file cannot be found.");

				return false;
			}

			return true;
		}

		public static void Main(string[] args) {
			string path;

			if (!Program.CheckArgs(args, out path)) return;

			var contents = File.ReadAllBytes(path);
			var jit = new Interpreter();

			try {
				jit.Parse(contents);
			}
			catch (InvalidProgramFormatException) {
				Console.Write("Invalid program format.");
			}

			try {
				jit.Run();
			}
			catch (UnhandledProgramExceptionException) {
				Console.Write("Unhandled JIT exception.");
			}
		}
	}
}