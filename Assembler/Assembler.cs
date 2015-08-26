using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArkeOS.Architecture;

namespace ArkeOS.Assembler {
    public class Assembler {
        private Dictionary<string, ulong> labels;
        private Dictionary<string, ulong> defines;
        private string inputFile;
        private ulong baseAddress;
        private ulong currentOffset;

        public Assembler(string inputFile, ulong baseAddress) {
            this.labels = new Dictionary<string, ulong>();
            this.defines = new Dictionary<string, ulong>();
            this.baseAddress = baseAddress;
            this.inputFile = inputFile;
        }

        public byte[] Assemble() {
            var lines = File.ReadAllLines(this.inputFile).Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(@"//")).Select(l => l.Replace(" + ", "+").Replace(" * ", "*").Replace(" - ", "-"));

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {

                    this.DiscoverDefines(lines);
                    this.DiscoverLabelAddresses(lines);

                    foreach (var line in lines) {
                        var parts = line.Split(' ');

                        this.currentOffset = (ulong)stream.Position / 8;

                        if (parts[0] == "BASE") {
                            this.baseAddress = Helpers.ParseLiteral(parts[1]);
                        }
                        else if (parts[0] == "OFFSET") {
                            stream.Seek((long)Helpers.ParseLiteral(parts[1]) * 8, SeekOrigin.Begin);
                        }
                        else if (parts[0] == "LABEL") {

                        }
                        else if (parts[0] == "DEFINE") {

                        }
                        else if (parts[0] == "CONST") {
                            writer.Write(this.ParseParameter(parts[1], true).Literal);
                        }
                        else if (parts[0] == "STRING") {
                            var start = line.IndexOf("\"") + 1;
                            var end = line.LastIndexOf("\"");
                            var str = line.Substring(start, end - start);

                            str.PadRight(str.Length + str.Length % 8, '\0');

                            writer.Write(Encoding.UTF8.GetBytes(str));
                        }
                        else {
                            this.ParseInstruction(parts, true).Encode(writer);
                        }
                    }

                    return stream.ToArray();
                }
            }
        }

        private void DiscoverLabelAddresses(IEnumerable<string> lines) {
            var address = this.baseAddress;

            foreach (var line in lines) {
                var parts = line.Split(' ');

                if (parts[0] == "BASE") {
                    this.baseAddress = Helpers.ParseLiteral(parts[1]);
                }
                else if (parts[0] == "OFFSET") {
                    address = Helpers.ParseLiteral(parts[1]) + this.baseAddress;
                }
                else if (parts[0] == "LABEL") {
                    this.labels.Add(parts[1], address);
                }
                else if (parts[0] == "DEFINE") {

                }
                else if (parts[0] == "CONST") {
                    address += 1;
                }
                else if (parts[0] == "STRING") {
                    var start = line.IndexOf("\"") + 1;
                    var end = line.LastIndexOf("\"");

                    address += (ulong)(end - start);
                }
                else {
                    address += this.ParseInstruction(parts, false).Length;
                }
            }
        }

        private void DiscoverDefines(IEnumerable<string> lines) {
            foreach (var line in lines) {
                var parts = line.Split(' ');

                if (parts[0] == "DEFINE")
                    this.defines.Add(parts[1], Helpers.ParseLiteral(parts[2]));
            }
        }

        private Instruction ParseInstruction(string[] parts, bool resolveLabels) {
            Parameter conditional = null;
            bool conditionalZero = false;

            var conditionalParts = parts[0].Split(':');
            if (conditionalParts.Length != 1) {
                parts[0] = conditionalParts[0];
                conditionalZero = conditionalParts[1] == "Z";
                conditional = this.ParseParameter(conditionalParts[2], resolveLabels);
            }

            var def = InstructionDefinition.Find(parts[0]);

            if (def == null)
                throw new InvalidInstructionException();

            return new Instruction(def.Code, parts.Skip(1).Select(p => this.ParseParameter(p, resolveLabels)).ToList(), conditional, conditionalZero);
        }

        private Parameter ParseParameter(string value, bool resolveLabels) {
            var isIndirect = value[0] == '[';

            if (isIndirect)
                value = value.Substring(1, value.Length - 2);

            if (value[0] == '(') {
                value = value.Substring(1, value.Length - 2);

                var parts = value.Split('+', '-', '*');
                var @base = new Parameter.Calculated(this.ParseParameter(parts[0], resolveLabels), true);
                var index = parts.Length > 1 ? new Parameter.Calculated(this.ParseParameter(parts[1], resolveLabels), value[parts[0].Length] == '+') : null;
                var scale = parts.Length > 2 ? new Parameter.Calculated(this.ParseParameter(parts[2], resolveLabels), true) : null;
                var offset = parts.Length > 3 ? new Parameter.Calculated(this.ParseParameter(parts[3], resolveLabels), value[parts[2].Length] == '+') : null;

                return this.ReduceCalculated(Parameter.CreateCalculated(isIndirect, @base, index, scale, offset));
            }
            else if (value[0] == '{') {
                return Parameter.CreateLiteral(isIndirect, resolveLabels ? this.labels[value.Substring(1, value.Length - 2)] : 0);
            }
            else if (value[0] == '0') {
                return Parameter.CreateLiteral(isIndirect, Helpers.ParseLiteral(value));
            }
            else if (value[0] == 'R') {
                return Parameter.CreateRegister(isIndirect, Helpers.ParseEnum<Register>(value));
            }
            else if (value == "S") {
                return Parameter.CreateStack(isIndirect);
            }
            else if (value[0] == '#') {
                value = value.Substring(1);

                if (value == "ADDRESS") {
                    return Parameter.CreateLiteral(isIndirect, this.currentOffset + this.baseAddress);
                }
                else if (value == "OFFSET") {
                    return Parameter.CreateLiteral(isIndirect, this.currentOffset);
                }
                else if (value == "BASE") {
                    return Parameter.CreateLiteral(isIndirect, this.baseAddress);
                }
                else {
                    return Parameter.CreateLiteral(isIndirect, this.defines[value]);
                }
            }
            else if (value[0] == '$') {
                value = value.Substring(1);

                var literal = 0UL;
                var command = value.Substring(0, value.IndexOf('('));
                var parameter = value.Substring(command.Length + 1, value.IndexOf(')') - command.Length - 1);

                if (resolveLabels) {
                    if (command == "DISTANCETO") {
                        var label = this.labels[parameter];

                        literal = label > this.currentOffset ? label - this.currentOffset : this.currentOffset - label;
                    }
                    else {
                        return null;
                    }
                }

                return Parameter.CreateLiteral(isIndirect, literal);
            }
            else {
                return null;
            }
        }

        private Parameter ReduceCalculated(Parameter calculated) {
            Func<Parameter.Calculated, bool> valid = c => c == null || (c.Parameter.Type == ParameterType.Literal && c.Parameter.IsIndirect == false);

            if (!valid(calculated.Base) || !valid(calculated.Index) || !valid(calculated.Scale) || !valid(calculated.Offset))
                return calculated;

            var value = calculated.Base.Parameter.Literal;

            if (calculated.Index != null) {
                var calc = calculated.Index.Parameter.Literal;

                if (calculated.Scale != null)
                    calc *= calculated.Scale.Parameter.Literal;

                if (calculated.Index.IsPositive) {
                    value += calc;
                }
                else {
                    value -= calc;
                }
            }

            if (calculated.Offset != null) {
                var calc = calculated.Offset.Parameter.Literal;

                if (calculated.Offset.IsPositive) {
                    value += calc;
                }
                else {
                    value -= calc;
                }
            }

            return Parameter.CreateLiteral(calculated.IsIndirect, value);
        }
    }
}