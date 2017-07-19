using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.IR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler.Emit {
    public sealed class Emitter {
        private readonly Dictionary<FunctionSymbol, ulong> functionOffsets = new Dictionary<FunctionSymbol, ulong>();
        private readonly Dictionary<GlobalVariableSymbol, ulong> globalVariableOffsets = new Dictionary<GlobalVariableSymbol, ulong>();
        private readonly List<Function> functions = new List<Function>();
        private readonly List<Instruction> instructions = new List<Instruction>();

        private readonly CompilationOptions options;
        private readonly Compiliation compilation;

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));

        public static void Emit(CompilationOptions options, Compiliation tree) => new Emitter(options, tree).Emit();

        private Emitter(CompilationOptions options, Compiliation compilation) => (this.options, this.compilation) = (options, compilation);

        private void Emit() {
            var entry = this.compilation.Functions.Single(f => f.Symbol.Name == "main");

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)entry.Symbol.LocalVariables.Count + 0x1_0000));
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Parameter.CreateLiteral(0x1_0000));

            var current = (ulong)this.instructions.Sum(i => i.Length);

            this.GenerateGlobalVariables();

            foreach (var f in this.compilation.Functions) {
                var func = new Function(f);

                func.Emit();

                this.functions.Add(func);

                this.functionOffsets[f.Symbol] = current;

                current += func.Length;
            }

            foreach (var f in this.functions) {
                f.Fixup(this.functionOffsets, this.globalVariableOffsets);

                this.instructions.AddRange(f.Instructions);
            }

            if (this.options.EmitAssemblyListing)
                this.GenerateListing();

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    if (this.options.EmitBootable)
                        writer.Write(0x00000000454B5241UL);

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(this.options.OutputName, stream.ToArray());
                }
            }
        }

        private void GenerateGlobalVariables() {
            var next = 0UL;

            foreach (var g in this.compilation.GlobalVariables)
                this.globalVariableOffsets[g] = next++;
        }

        private void GenerateListing() {
            string hexFormatString(ulong max) => "X" + (byte)Math.Ceiling(Math.Log(max, 16));

            var str = new StringBuilder();

            foreach (var i in this.globalVariableOffsets)
                str.AppendLine($"gvar {i.Key.Name}: 0x{i.Value:X16}");

            if (this.globalVariableOffsets.Any())
                str.AppendLine();

            foreach (var f in this.functions) {
                str.AppendLine($"func {f.Source.Symbol.Name}: 0x{functionOffsets[f.Source.Symbol]:X16}");

                var stackFormat = hexFormatString(f.Source.Symbol.StackRequired);
                var instrFormat = hexFormatString(f.Length);

                foreach (var a in f.Source.Symbol.Arguments)
                    str.AppendLine($"arg {a.Name}: 0x{f.Source.Symbol.GetStackPosition(a).ToString(stackFormat)}");

                foreach (var a in f.Source.Symbol.LocalVariables)
                    str.AppendLine($"var {a.Name}: 0x{f.Source.Symbol.GetStackPosition(a).ToString(stackFormat)}");

                var offset = 0UL;
                foreach (var i in f.Instructions) {
                    str.AppendLine($"0x{offset.ToString(instrFormat)}: {i}");
                    offset += i.Length;
                }

                str.AppendLine();
            }

            File.WriteAllText(Path.ChangeExtension(this.options.OutputName, "lst"), str.ToString());
        }
    }
}
