using System;
using System.IO;
using ArkeOS.Executable;
using ArkeOS.ISA;

namespace ArkeOS.Assembler {
	public static class Program {
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
						else if (dataSection != null) {
							dataSection.AddLiteral(parts, pendingLabel);
						}
						else {
							throw new InvalidDirectiveOutsideSectionException();
						}

						pendingLabel = string.Empty;

						break;

				}
			}

			return image.ToArray();
		}

		public static void Main(string[] args) {
			Console.Write("Input file: ");

			var input = Console.ReadLine();

			if (!input.EndsWith(".asm"))
				input += ".asm";

			if (!input.Contains("\\"))
				input = @"D:\Code\ArkeOS\Images\" + input;

			var output = Path.ChangeExtension(input, "aoe");

			if (File.Exists(input)) {
				File.WriteAllBytes(output, Program.Assemble(input));
			}
			else {
				Console.WriteLine("The specified file cannot be found.");
			}
		}
	}
}