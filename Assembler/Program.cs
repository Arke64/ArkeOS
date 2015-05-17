using System;
using System.IO;
using ArkeOS.Executable;

namespace ArkeOS.Assembler {
	public static class Program {
		private static bool CheckArgs(string[] args, out string input, out string output) {
			input = string.Empty;
			output = string.Empty;

			if (args.Length != 2) {
				input = "../../Program.asm";
				output = "../../../Interpreter/Program.aoe";
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

		public static void Main(string[] args) {
			string input, output;

			if (!Program.CheckArgs(args, out input, out output)) {
				Console.ReadLine();

				return;
			}

			Section section = null;
			var image = new Image();
			var lines = File.ReadAllLines(input);

			image.Header.Magic = Header.MagicNumber;

			foreach (var line in lines) {
				var parts = line.Split(' ');

				switch (parts[0]) {
					case "ENTRY": image.Header.EntryPointAddress = Convert.ToUInt64(parts[1], 16); break;
					case "STACK": image.Header.StackAddress = Convert.ToUInt64(parts[1], 16); break;
					case "ORIGIN":
						section = new Section(Convert.ToUInt64(parts[1], 16));

						image.Sections.Add(section);

						break;

					case "LABEL":

						break;

					default:
						if (string.IsNullOrWhiteSpace(parts[0]))
							continue;

						section.AddInstruction(new Instruction(parts));

						break;
				}
			}

			image.Header.SectionCount = (ushort)image.Sections.Count;

			File.WriteAllBytes(output, image.ToArray());
		}
	}
}