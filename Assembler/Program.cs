using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		private static void Write(this BinaryWriter writer, Instruction instruction) {
			writer.Write((ushort)instruction);
		}

		public static void Main(string[] args) {
			string input, output;

			if (!Program.CheckArgs(args, out input, out output)) {
				Console.ReadLine();

				return;
			}

			var lines = File.ReadAllLines(input);
			var image = new Image();
			var sections = new List<List<string[]>>();

			image.Header.Magic = Header.MagicNumber;
			image.Header.EntryPointAddress = Convert.ToUInt64(lines.Single(l => l.IndexOf("ENTRY") == 0).Split(' ')[1], 16);
			image.Header.StackAddress = Convert.ToUInt64(lines.Single(l => l.IndexOf("STACK") == 0).Split(' ')[1], 16);

			for (var i = 2; i < lines.Length; i++) {
				var parts = lines[i].Split(' ');

				if (parts[0] == "CODE") {
					sections.Add(new List<string[]>());
					image.Sections.Add(new Section() { Address = Convert.ToUInt64(parts[1], 16) });

					continue;
				}
				else if (string.IsNullOrWhiteSpace(parts[0])) {
					continue;
				}
				else {
					sections.Last().Add(parts);
				}
			}

			image.Header.SectionCount = (ushort)sections.Count;

			for (var i = 0; i < sections.Count; i++) {
				using (var stream = new MemoryStream()) {
					using (var writer = new BinaryWriter(stream)) {
						foreach (var parts in sections[i]) {
							switch (parts[0]) {
								case "HALT":
									writer.Write(Instruction.Halt);

									break;

								case "NOP":
									writer.Write(Instruction.Nop);

									break;

								default:
									throw new Exception();
							}
						}

						image.Sections[i].Size = (ulong)writer.BaseStream.Length;
					}

					image.Sections[i].Data = stream.ToArray();
				}
			}

			File.WriteAllBytes(output, image.ToArray());
		}
	}
}
