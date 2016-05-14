using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities;

namespace ArkeOS.Tools.Assembler {
    public class Assembler {
        private Dictionary<string, ulong> labels;
        private Dictionary<string, ulong> defines;
        private Dictionary<string, ulong> variables;
        private string inputFile;
        private ulong baseAddress;
        private ulong currentOffset;
        private bool positionIndependent;

        public Assembler(string inputFile, ulong baseAddress) {
            this.labels = new Dictionary<string, ulong>();
            this.defines = new Dictionary<string, ulong>();
            this.variables = new Dictionary<string, ulong>();
            this.baseAddress = baseAddress;
            this.inputFile = inputFile;
            this.positionIndependent = false;
        }

        public byte[] Assemble() {
            var lines = File.ReadAllLines(this.inputFile).Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(@"//")).Select(l => l.Replace(" + ", "+").Replace(" * ", "*").Replace(" - ", "-"));

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {

                    this.DiscoverAddresses(lines);

                    foreach (var line in lines) {
                        var parts = line.Split(' ');

                        this.currentOffset = (ulong)stream.Position / 8;

                        if (parts[0] == "BASE") {
                            this.baseAddress = Helpers.ParseLiteral(parts[1]);
                        }
                        else if (parts[0] == "PIC") {
                            this.positionIndependent = true;
                        }
                        else if (parts[0] == "OFFSET") {
                            stream.Seek((long)Helpers.ParseLiteral(parts[1]) * 8, SeekOrigin.Begin);
                        }
                        else if (parts[0] == "LABEL") {

                        }
                        else if (parts[0] == "DEFINE") {

                        }
                        else if (parts[0] == "VAR") {
                            writer.Write(this.ParseParameter(parts[2], true).Address);
                        }
                        else if (parts[0] == "CONST") {
                            writer.Write(this.ParseParameter(parts[1], true).Address);
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

        private void DiscoverAddresses(IEnumerable<string> lines) {
            var address = this.baseAddress;

            foreach (var line in lines) {
                var parts = line.Split(' ');

                if (parts[0] == "BASE") {
                    this.baseAddress = Helpers.ParseLiteral(parts[1]);
                }
                else if (parts[0] == "PIC") {
                    this.positionIndependent = true;
                }
                else if (parts[0] == "OFFSET") {
                    address = Helpers.ParseLiteral(parts[1]) + this.baseAddress;
                }
                else if (parts[0] == "LABEL") {
                    this.labels.Add(parts[1], address);
                }
                else if (parts[0] == "DEFINE") {
                    this.defines.Add(parts[1], Helpers.ParseLiteral(parts[2]));
                }
                else if (parts[0] == "VAR") {
                    this.variables.Add(parts[1], address);

                    address += 1;
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

        private Instruction ParseInstruction(string[] parts, bool resolveNames) {
            Parameter conditional = null;
            var conditionalZero = false;
            var skip = 0;

            if (parts[0] == "IFZ" || parts[0] == "IFNZ") {
                conditionalZero = parts[0] == "IFZ";
                conditional = this.ParseParameter(parts[1], resolveNames);
                skip = 2;
            }

            var def = InstructionDefinition.Find(parts[skip]);

            if (def == null)
                throw new InvalidInstructionException();

            return new Instruction(def.Code, parts.Skip(skip + 1).Select(p => this.ParseParameter(p, resolveNames)).ToList(), conditional, conditionalZero);
        }

        private Parameter ParseParameter(string value, bool resolveNames) {
            var isIndirect = value[0] == '[';

            if (isIndirect)
                value = value.Substring(1, value.Length - 2);

            if (value[0] == '(') {
                value = value.Substring(1, value.Length - 2);

                var parts = value.Split('+', '-', '*');
                var @base = new Parameter.Calculated(true, this.ParseParameter(parts[0], resolveNames));
                var index = parts.Length > 1 ? new Parameter.Calculated(value[parts[0].Length] == '+', this.ParseParameter(parts[1], resolveNames)) : null;
                var scale = parts.Length > 2 ? new Parameter.Calculated(true, this.ParseParameter(parts[2], resolveNames)) : null;
                var offset = parts.Length > 3 ? new Parameter.Calculated(value[parts[2].Length] == '+', this.ParseParameter(parts[3], resolveNames)) : null;

                return this.ReduceCalculated(Parameter.CreateCalculated(isIndirect, false, @base, index, scale, offset));
            }
            else if (value[0] == '0') {
                return Parameter.CreateAddress(isIndirect, false, Helpers.ParseLiteral(value));
            }
            else if (value[0] == 'R') {
                return Parameter.CreateRegister(isIndirect, false, Helpers.ParseEnum<Register>(value));
            }
            else if (value == "S") {
                return Parameter.CreateStack(isIndirect, false);
            }
            else if (value[0] == '$') {
                value = value.Substring(1);

                if (!resolveNames)
                    return Parameter.CreateAddress(isIndirect, false, 0);

                if (value == "Address") {
                    return Parameter.CreateAddress(isIndirect, false, this.currentOffset + this.baseAddress);
                }
                else if (value == "Offset") {
                    return Parameter.CreateAddress(isIndirect, false, this.currentOffset);
                }
                else if (value == "Base") {
                    return Parameter.CreateAddress(isIndirect, false, this.baseAddress);
                }
                else if (this.defines.ContainsKey(value)) {
                    return Parameter.CreateAddress(isIndirect, false, this.defines[value]);
                }
                else if (this.variables.ContainsKey(value)) {
                    return Parameter.CreateAddress(isIndirect, this.positionIndependent, unchecked(this.variables[value] - (this.positionIndependent ? this.currentOffset : 0)));
                }
                else if (this.labels.ContainsKey(value)) {
                    return Parameter.CreateAddress(isIndirect, this.positionIndependent, unchecked(this.labels[value] - (this.positionIndependent ? this.currentOffset : 0)));
                }
                else {
                    throw new VariableNotFoundException();
                }
            }
            else {
                throw new InvalidParameterException();
            }
        }

        private Parameter ReduceCalculated(Parameter calculated) {
            Func<Parameter.Calculated, bool> valid = c => c == null || (c.Parameter.Type == ParameterType.Address && c.Parameter.IsIndirect == false);

            if (!valid(calculated.Base) || !valid(calculated.Index) || !valid(calculated.Scale) || !valid(calculated.Offset))
                return calculated;

            var value = calculated.Base.Parameter.Address;

            if (calculated.Index != null) {
                var calc = calculated.Index.Parameter.Address;

                if (calculated.Scale != null)
                    calc *= calculated.Scale.Parameter.Address;

                if (calculated.Index.IsPositive) {
                    value += calc;
                }
                else {
                    value -= calc;
                }
            }

            if (calculated.Offset != null) {
                var calc = calculated.Offset.Parameter.Address;

                if (calculated.Offset.IsPositive) {
                    value += calc;
                }
                else {
                    value -= calc;
                }
            }

            return Parameter.CreateAddress(calculated.IsIndirect, calculated.IsRIPRelative, value);
        }
    }
}