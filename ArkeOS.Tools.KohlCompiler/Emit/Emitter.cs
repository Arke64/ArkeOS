using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.IR;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler.Emit {
    public sealed class Emitter {
        private readonly Dictionary<FunctionSymbol, ulong> functionOffsets = new Dictionary<FunctionSymbol, ulong>();
        private readonly Dictionary<GlobalVariableSymbol, ulong> variableOffsets = new Dictionary<GlobalVariableSymbol, ulong>();

        private readonly List<Instruction> instructions = new List<Instruction>();
        private readonly List<Function> functions = new List<Function>();

        private readonly CompilationOptions options;
        private readonly Compiliation tree;

        public static void Emit(CompilationOptions options, Compiliation tree) => new Emitter(options, tree).Emit();

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));

        private Emitter(CompilationOptions options, Compiliation tree) => (this.options, this.tree) = (options, tree);

        private void Emit() {
            this.EmitHeader();

            var current = (ulong)this.instructions.Sum(i => i.Length);
            var next = 0UL;
            foreach (var g in this.tree.GlobalVariables)
                this.variableOffsets[g] = next++;

            foreach (var f in this.tree.Functions) {
                var func = new Function(f, this.variableOffsets);

                func.Emit();

                this.functions.Add(func);

                this.functionOffsets[f.Symbol] = current;

                current += func.Length;
            }

            foreach (var f in this.functions) {
                f.Fixup(this.functionOffsets);

                this.instructions.AddRange(f.Instructions);
            }

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    if (this.options.EmitBootable)
                        writer.Write(0x00000000454B5241UL);

                    if (this.options.EmitAssemblyListing) {
                        var str = new StringBuilder();

                        foreach (var i in this.variableOffsets)
                            str.AppendLine($"{i.Key.Name}: 0x{i.Value:X16}");

                        if (this.variableOffsets.Any())
                            str.AppendLine();

                        foreach (var f in this.functions) {
                            str.AppendLine($"{f.Source.Symbol.Name}: 0x{this.functionOffsets[f.Source.Symbol]:X16}");

                            var para = 0;
                            var formatter = "X" + ((f.Source.Symbol.Arguments.Count + f.Source.Symbol.LocalVariables.Count) / 16);

                            foreach (var a in f.Source.Symbol.Arguments)
                                str.AppendLine($"arg {a.Name}: 0x{(para++).ToString(formatter)}");

                            foreach (var a in f.Source.Symbol.LocalVariables)
                                str.AppendLine($"var {a.Name}: 0x{(para++).ToString(formatter)}");

                            var cur = 0UL;
                            foreach (var i in f.Instructions) {
                                str.AppendLine($"0x{cur.ToString("X" + (f.Length / 16))}: {i}");
                                cur += i.Length;
                            }

                            str.AppendLine();
                        }

                        File.WriteAllText(Path.ChangeExtension(this.options.OutputName, "lst"), str.ToString());
                    }

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(this.options.OutputName, stream.ToArray());
                }
            }
        }

        private void EmitHeader() {
            var entry = this.tree.Functions.Single(f => f.Symbol.Name == "main");

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)entry.Symbol.LocalVariables.Count + 0x1_0000));
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Parameter.CreateLiteral(0x1_0000));
        }
    }
}
