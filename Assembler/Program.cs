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

		public static void Main(string[] args) {
			string input, output;

			if (!Program.CheckArgs(args, out input, out output)) {
				Console.ReadLine();

				return;
			}

			var lines = File.ReadAllLines(input);
			var image = new Image();
			var sections = new Dictionary<Section, List<string[]>>();

			image.Header.Magic = Header.MagicNumber;

			foreach (var line in lines) {
				var parts = line.Split(' ');

				switch (parts[0]) {
					case "ENTRY": image.Header.EntryPointAddress = Convert.ToUInt64(parts[1], 16); break;
					case "STACK": image.Header.StackAddress = Convert.ToUInt64(parts[1], 16); break;

					case "CODE":
						var section = new Section() { Address = Convert.ToUInt64(parts[1], 16) };

						image.Sections.Add(section);
						sections.Add(section, new List<string[]>());

						break;

					default:
						if (string.IsNullOrWhiteSpace(parts[0]))
							continue;

						sections.Last().Value.Add(parts);

						break;
				}
			}

			foreach (var s in sections) {
				var section = s.Key;

				using (var stream = new MemoryStream()) {
					using (var writer = new BinaryWriter(stream)) {
						foreach (var parts in s.Value) {
							var def = Instruction.Find(parts[0]);
							var parameters = parts.Skip(1).Select(p => new Parameter(p));
							var state = 0;

							if (def == null) throw new InvalidInstructionException();
							if (parameters.Any(p => !p.IsValid)) throw new InvalidParameterException();
							if (parameters.Count() > 3) throw new InvalidParameterException();
							if (parameters.Count(p => !p.IsRegister) > 1) throw new InvalidParameterException();
							if (parameters.Count() > 0 && !parameters.Last().IsRegister && parameters.Count(p => !p.IsRegister) > 1) throw new InvalidParameterException();

							foreach (var p in parameters) {
								if (p.IsRegister) {
									switch (state++) {
										case 0: def.Ra = p.Register; break;
										case 1: def.Rb = p.Register; break;
										case 2: def.Rc = p.Register; break;
									}
								}
								else {
									def.Value = p.Literal;
								}
							}

							def.Serialize(writer);
						}
					}

					section.Data = stream.ToArray();
					section.Size = (ulong)section.Data.LongLength;
				}
			}

			image.Header.SectionCount = (ushort)sections.Count;

			File.WriteAllBytes(output, image.ToArray());
		}
	}
}