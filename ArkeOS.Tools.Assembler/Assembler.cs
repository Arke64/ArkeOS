using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ArkeOS.Tools.Assembler {
    public class Assembler {
        private Dictionary<string, ulong> labels;
        private Dictionary<string, ulong> defines;
        private Dictionary<string, ulong> variables;
        private Dictionary<string, ulong> strings;
        private ulong currentOffset;

        public Assembler() {
            this.labels = new Dictionary<string, ulong>();
            this.defines = new Dictionary<string, ulong>();
            this.variables = new Dictionary<string, ulong>();
            this.strings = new Dictionary<string, ulong>();
            this.currentOffset = 0;
        }

        private static string Sanitize(string input) => Regex.Replace(input, @"\s+", " ").Replace("+ ", "+").Replace(" +", "+").Replace("- ", "-").Replace(" -", "-").Replace("* ", "*").Replace(" *", "*");

        public byte[] Assemble(string sourceFolder, string[] inputLines) {
            IEnumerable<string> lines = inputLines.ToList();

            while (this.ProcessIncludes(sourceFolder, ref lines))
                ;

            lines = lines.Select(l => l.Split(new string[] { "//" }, StringSplitOptions.None)[0].Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => Assembler.Sanitize(l));

            this.DiscoverDefines(lines);
            this.DiscoverAddresses(lines);

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    foreach (var line in lines) {
                        var parts = line.Split(' ');

                        this.currentOffset = (ulong)stream.Position / 8;

                        if (parts[0] == "OFFSET") {
                            stream.Seek((long)this.ParseParameter(parts[1], true).Literal * 8, SeekOrigin.Begin);
                        }
                        else if (parts[0] == "LABEL") {

                        }
                        else if (parts[0] == "DEFINE") {

                        }
                        else if (parts[0] == "VAR") {
                            writer.Write(this.ParseParameter(parts[2], true).Literal);
                        }
                        else if (parts[0] == "CONST") {
                            writer.Write(this.ParseParameter(parts[1], true).Literal);
                        }
                        else if (parts[0] == "STR") {
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

        private bool ProcessIncludes(string sourceFolder, ref IEnumerable<string> lines) {
            var result = new List<string>();
            var last = 0;
            var i = 0;

            for (; i < lines.Count(); i++) {
                var line = lines.ElementAt(i);

                if (line.StartsWith("INCLUDE")) {
                    result.AddRange(lines.Skip(last).Take(i - last));

                    result.AddRange(File.ReadAllLines(Path.Combine(sourceFolder, line.Substring(line.IndexOf(' ') + 1))));

                    last = i + 1;
                }
            }

            if (result.Count == 0)
                return false;

            result.AddRange(lines.Skip(last).Take(i - last));

            lines = result;

            return true;
        }

        private void DiscoverDefines(IEnumerable<string> lines) {
            var indirectDefines = new Dictionary<string, string>();

            foreach (var line in lines) {
                var parts = line.Split(' ');

                if (parts[0] != "DEFINE")
                    continue;

                if (parts[2].StartsWith("$")) {
                    indirectDefines.Add(parts[1], parts[2].Substring(1));
                }
                else {
                    this.defines.Add(parts[1], Helpers.ParseLiteral(parts[2]));
                }
            }

            this.ReduceDefines(indirectDefines);
        }

        private void ReduceDefines(Dictionary<string, string> indirect) {
            var remaining = new Dictionary<string, string>();
            var failed = false;

            foreach (var d in indirect) {
                if (this.defines.ContainsKey(d.Key)) {
                    this.defines.Add(d.Key, this.defines[d.Key]);
                }
                else {
                    remaining.Add(d.Key, d.Value);
                    failed = true;
                }
            }

            if (failed)
                this.ReduceDefines(remaining);
        }

        private void DiscoverAddresses(IEnumerable<string> lines) {
            this.currentOffset = 0;

            foreach (var line in lines) {
                var parts = line.Split(' ');

                if (parts[0] == "OFFSET") {
                    this.currentOffset = this.ParseParameter(parts[1], false).Literal;
                }
                else if (parts[0] == "LABEL") {
                    this.labels.Add(parts[1], this.currentOffset);
                }
                else if (parts[0] == "DEFINE") {

                }
                else if (parts[0] == "VAR") {
                    this.variables.Add(parts[1], this.currentOffset);

                    this.currentOffset += 1;
                }
                else if (parts[0] == "CONST") {
                    this.currentOffset += 1;
                }
                else if (parts[0] == "STR") {
                    var start = line.IndexOf("\"") + 1;
                    var end = line.LastIndexOf("\"");
                    var len = end - start;

                    this.strings.Add(parts[1], this.currentOffset);

                    this.currentOffset += (ulong)(len + len % 8);
                }
                else {
                    this.currentOffset += this.ParseInstruction(parts, false).Length;
                }
            }
        }

        private Instruction ParseInstruction(string[] parts, bool resolveNames) {
            var conditional = default(Parameter);
            var conditionalZero = false;
            var skip = 0;

            if (parts[0] == "IFZ" || parts[0] == "IFNZ") {
                conditionalZero = parts[0] == "IFZ";
                conditional = this.ParseParameter(parts[1], resolveNames);
                skip += 2;
            }

            var def = InstructionDefinition.Find(parts[skip++]);

            if (def == null)
                throw new InvalidInstructionException();

            return new Instruction(def.Code, parts.Skip(skip).Select(p => this.ParseParameter(p, resolveNames)).ToList(), conditional, conditionalZero);
        }

        private Parameter ParseParameter(string value, bool resolveNames) {
            var isIndirect = value[0] == '[';

            if (isIndirect)
                value = value.Substring(1, value.Length - 2);

            if (value[0] == '(') {
                value = value.Substring(1, value.Length - 2);

                var parts = value.Split('+', '*');

                if (parts[0][0] == '-') throw new InvalidParameterException("Base can't be negative.");

                var @base = new Parameter.Calculated(true, this.ParseParameter(parts[0], resolveNames));
                var index = parts.Length > 1 ? new Parameter.Calculated(parts[1][0] != '-', this.ParseParameter(parts[1].TrimStart('-'), resolveNames)) : null;
                var scale = parts.Length > 2 ? new Parameter.Calculated(parts[2][0] != '-', this.ParseParameter(parts[2].TrimStart('-'), resolveNames)) : null;
                var offset = parts.Length > 3 ? new Parameter.Calculated(parts[3][0] != '-', this.ParseParameter(parts[3].TrimStart('-'), resolveNames)) : null;

                return this.ReduceCalculated(Parameter.CreateCalculated(@base, index, scale, offset, isIndirect, false));
            }
            else if (value[0] == '0') {
                return Parameter.CreateLiteral(Helpers.ParseLiteral(value), isIndirect, false);
            }
            else if (value[0] == 'R') {
                return Parameter.CreateRegister(Helpers.ParseEnum<Register>(value), isIndirect, false);
            }
            else if (value == "S") {
                return Parameter.CreateStack(isIndirect, false);
            }
            else if (value[0] == '$') {
                value = value.Substring(1);

                if (value == "Offset") {
                    return Parameter.CreateLiteral(this.currentOffset, isIndirect, false);
                }
                else if (this.defines.ContainsKey(value)) {
                    var literal = this.defines[value];

                    if (literal > 1 && literal != ulong.MaxValue) {
                        return Parameter.CreateLiteral(literal, isIndirect, false);
                    }
                    else {
                        return Parameter.CreateRegister(literal == 0 ? Register.RZERO : (literal == 1 ? Register.RONE : Register.RMAX), isIndirect, false);
                    }
                }

                if (!resolveNames)
                    return Parameter.CreateLiteral(0, isIndirect, false);

                if (this.variables.ContainsKey(value)) {
                    return Parameter.CreateLiteral(unchecked(this.variables[value] - this.currentOffset), isIndirect, true);
                }
                else if (this.strings.ContainsKey(value)) {
                    return Parameter.CreateLiteral(unchecked(this.strings[value] - this.currentOffset), isIndirect, true);
                }
                else if (this.labels.ContainsKey(value)) {
                    return Parameter.CreateLiteral(unchecked(this.labels[value] - this.currentOffset), isIndirect, true);
                }

                throw new VariableNotFoundException(value);
            }
            else {
                throw new InvalidParameterException(value);
            }
        }

        private Parameter ReduceCalculated(Parameter calculated) {
            bool valid(Parameter.Calculated c) => c == null || (c.Parameter.Type == ParameterType.Literal && !c.Parameter.IsIndirect);

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

            return Parameter.CreateLiteral(value, calculated.IsIndirect, calculated.IsRIPRelative);
        }
    }
}
