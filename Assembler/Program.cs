using System;
using System.IO;
using ArkeOS.Executable;
using ArkeOS.ISA;

namespace ArkeOS.Assembler {
	public static class Program {
		private static bool CheckArgs(string[] args, out string input, out string output) {
			input = string.Empty;
			output = string.Empty;

			if (args.Length != 2) {
				input = "../../Program.asm";
				output = "../../../Virtual Machine/Program.aoe";
			}
			else {
				input = args[0];
				output = args[1];
			}

			if (!File.Exists(input)) {
				Console.WriteLine("The specified file cannot be found.");

				return false;
			}

			return true;
		}

		private static byte[] Assemble(string input) {
			CodeSection codeSection = null;
			DataSection dataSection = null;
			var image = new Image();
			var lines = File.ReadAllLines(input);
			var pendingLabel = string.Empty;

			foreach (var line in lines) {
				var parts = line.Split(' ');

				switch (parts[0]) {
					case "ENTRY":
						image.Header.EntryPointAddress = Convert.ToUInt64(parts[1], 16);

						break;

					case "STACK":
						image.Header.StackAddress = Convert.ToUInt64(parts[1], 16);

						break;

					case "CODE":
						codeSection = new CodeSection(image, Convert.ToUInt64(parts[1], 16));
						dataSection = null;

						image.Sections.Add(codeSection);

						break;

					case "DATA":
						dataSection = new DataSection(image, Convert.ToUInt64(parts[1], 16));
						codeSection = null;

						image.Sections.Add(dataSection);

						break;

					case "LABEL":
						pendingLabel = parts[1];

						break;

					default:
						if (string.IsNullOrWhiteSpace(parts[0]) || parts[0].TrimStart().IndexOf("//") == 0)
							continue;

						if (codeSection != null) {
							codeSection.AddInstruction(new Instruction(parts), pendingLabel);
						}
						else {
							dataSection.AddLiteral(parts, pendingLabel);
						}

						pendingLabel = string.Empty;

						break;

				}
			}

			return image.ToArray();
		}

		public static void Main(string[] args) {
			string input, output;

			if (!Program.CheckArgs(args, out input, out output))
				return;

			File.WriteAllBytes(output, Program.Assemble(input));
		}
	}
}