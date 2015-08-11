using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArkeOS.ISA;

namespace ArkeOS.Assembler {
	public class Assembler {
		private Dictionary<string, List<Tuple<ulong, InstructionSize>>> labelReferences;
		private Dictionary<string, ulong> labels;
		private string inputFile;

		public Assembler(string inputFile) {
			this.labelReferences = new Dictionary<string, List<Tuple<ulong, InstructionSize>>>();
			this.labels = new Dictionary<string, ulong>();
			this.inputFile = inputFile;
		}

		public byte[] Assemble() {
			var lines = File.ReadAllLines(this.inputFile).Where(l => !string.IsNullOrWhiteSpace(l));

			using (var stream = new MemoryStream()) {
				using (var writer = new BinaryWriter(stream)) {

					foreach (var label in lines.Where(l => l.StartsWith("LABEL")).Select(l => l.Substring(l.IndexOf(" ") + 1)))
						this.labelReferences.Add(label, new List<Tuple<ulong, InstructionSize>>());

					foreach (var line in lines) {
						var parts = line.Split(' ');

						if (parts[0] == "ORIGIN") {
							stream.Seek((long)Parameter.ParseLiteral(parts[1]), SeekOrigin.Begin);
						}
						else if (parts[0] == "LABEL") {
							this.labels.Add(parts[1], (ulong)stream.Position);
						}
						else if (parts[0].StartsWith("CONST")) {
							if (parts[1].StartsWith("0")) {
								Parameter.Write(writer, Parameter.ParseLiteral(parts[1]), Instruction.BytesToSize(int.Parse(parts[0].Split(':')[1])));
							}
							else {
								labelReferences[parts[1].Substring(1, parts[1].Length - 2).Trim()].Add(Tuple.Create((ulong)writer.BaseStream.Position, InstructionSize.EightByte));
							}
						}
						else if (parts[0] == "STRING") {
							var start = line.IndexOf("\"") + 1;
							var end = line.LastIndexOf("\"");

							writer.Write(Encoding.UTF8.GetBytes(line.Substring(start, end - start)));
						}
						else {
							var instruction = InstructionDefinition.Find(parts[0]);

							if (instruction == null)
								throw new InvalidDirectiveException();

							new Instruction(parts).Serialize(writer, this.labelReferences);
						}
					}

					foreach (var label in this.labelReferences) {
						var address = this.labels[label.Key];

						foreach (var reference in label.Value) {
							stream.Seek((long)reference.Item1, SeekOrigin.Begin);

							Parameter.Write(writer, address, reference.Item2);
						}
					}

					return stream.ToArray();
				}
			}
		}
	}
}