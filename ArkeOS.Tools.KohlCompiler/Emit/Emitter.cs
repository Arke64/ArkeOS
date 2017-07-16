using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public sealed class Function {
        private static Parameter StackParam { get; } = Parameter.CreateStack();
        private static Parameter RbpParam { get; } = Parameter.CreateRegister(Register.RBP);
        private static Parameter RspParam { get; } = Parameter.CreateRegister(Register.RSP);
        private static Parameter R0Param { get; } = Parameter.CreateRegister(Register.R0);

        private readonly Dictionary<BasicBlock, ulong> blockOffsets = new Dictionary<BasicBlock, ulong>();

        private readonly List<(Parameter, BasicBlock)> jumpFixups = new List<(Parameter, BasicBlock)>();
        private readonly List<(Parameter, FunctionSymbol)> callFixups = new List<(Parameter, FunctionSymbol)>();

        private readonly List<Instruction> instructions = new List<Instruction>();

        private readonly IReadOnlyDictionary<GlobalVariableSymbol, ulong> variableOffsets;
        private readonly FunctionSymbol currentFunction;

        private ulong length = 0;

        private void Add(Instruction i) {
            this.instructions.Add(i);
            this.length += i.Length;
        }

        public IReadOnlyCollection<Instruction> Instructions => this.instructions;
        public ulong Length => this.length;
        public IR.Function Source { get; }

        public Function(IR.Function source, IReadOnlyDictionary<GlobalVariableSymbol, ulong> globalVariables) => (this.Source, this.variableOffsets, this.currentFunction) = (source, globalVariables, source.Symbol);

        public void Emit() {
            foreach (var f in this.Source.AllBlocks)
                this.Visit(f);
        }

        public void Fixup(IReadOnlyDictionary<FunctionSymbol, ulong> functionOffsets) {
            var start = functionOffsets[this.currentFunction];

            foreach (var f in this.jumpFixups)
                f.Item1.Literal = this.blockOffsets[f.Item2] - f.Item1.Literal;

            foreach (var f in this.callFixups)
                f.Item1.Literal = functionOffsets[f.Item2] - (start + f.Item1.Literal);
        }

        private ulong CurrentOffset => (ulong)this.instructions.Sum(i => i.Length);

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void Visit(BasicBlock node) {
            if (!node.Instructions.Any() && node.Terminator == null) return;

            this.blockOffsets[node] = (ulong)this.instructions.Sum(i => i.Length);

            foreach (var i in node.Instructions) {
                switch (i) {
                    case BasicBlockAssignmentInstruction n: this.Visit(n); break;
                    case BasicBlockBinaryOperationInstruction n: this.Visit(n); break;
                    case BasicBlockIntrinsicInstruction n: this.Visit(n); break;
                    default: Debug.Assert(false); break;
                }
            }

            switch (node.Terminator) {
                case ReturnTerminator n: this.Visit(n); break;
                case CallTerminator n: this.Visit(n); break;
                case IfTerminator n: this.Visit(n); break;
                case GotoTerminator n: this.Visit(n); break;
                default: Debug.Assert(false); break;
            }
        }

        private Parameter GetParameter(RValue variable) {
            switch (variable) {
                case IntegerRValue v:
                    return Parameter.CreateLiteral(v.Value);

                case AddressOfRValue n:
                    return this.GetParameter(n.Target);

                case LValue v:
                    return this.GetParameter(v);

                default:
                    Debug.Assert(false);

                    return null;
            }
        }

        private Parameter GetParameter(LValue variable) {
            var addr = 0UL;

            switch (variable) {
                case LocalVariableLValue v:
                    var vidx = 0UL;
                    var localArg = this.currentFunction.LocalVariables.SkipWhile(a => { vidx++; return a != v.Symbol; }).FirstOrDefault();

                    if (localArg == null)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral((vidx + (ulong)this.currentFunction.Arguments.Count) - 1, ParameterFlags.RelativeToRBP | ParameterFlags.Indirect);

                case GlobalVariableLValue v:
                    if (!this.variableOffsets.TryGetValue(v.Symbol, out addr))
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral(addr, ParameterFlags.Indirect);

                case ArgumentLValue v:
                    var aidx = 0UL;
                    var arg = this.currentFunction.Arguments.SkipWhile(a => { aidx++; return a != v.Symbol; }).FirstOrDefault();

                    if (arg == null)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral(aidx - 1, ParameterFlags.RelativeToRBP | ParameterFlags.Indirect);

                case RegisterLValue v:
                    return Parameter.CreateRegister(v.Symbol.Register);

                case PointerLValue v:
                    var r = this.GetParameter(v.Target);

                    r.IsIndirect = true;

                    this.Emit(InstructionDefinition.SET, Function.StackParam, r);

                    return Parameter.CreateStack(ParameterFlags.Indirect);

                default:
                    Debug.Assert(false);

                    throw new InvalidOperationException();
            }
        }

        private void Visit(BasicBlockIntrinsicInstruction s) {
            if (s.Intrinsic.ParameterCount == 3) this.Emit(s.Intrinsic, this.GetParameter(s.Argument1), this.GetParameter(s.Argument2), this.GetParameter(s.Argument3));
            else if (s.Intrinsic.ParameterCount == 2) this.Emit(s.Intrinsic, this.GetParameter(s.Argument1), this.GetParameter(s.Argument2));
            else if (s.Intrinsic.ParameterCount == 1) this.Emit(s.Intrinsic, this.GetParameter(s.Argument1));
            else this.Emit(s.Intrinsic);
        }

        private void Visit(BasicBlockAssignmentInstruction a) => this.Emit(InstructionDefinition.SET, this.GetParameter(a.Target), this.GetParameter(a.Value));

        private void Visit(BasicBlockBinaryOperationInstruction n) {
            var def = default(InstructionDefinition);

            switch (n.Op) {
                case BinaryOperationType.Addition: def = InstructionDefinition.ADD; break;
                case BinaryOperationType.Subtraction: def = InstructionDefinition.SUB; break;
                case BinaryOperationType.Multiplication: def = InstructionDefinition.MUL; break;
                case BinaryOperationType.Division: def = InstructionDefinition.DIV; break;
                case BinaryOperationType.Remainder: def = InstructionDefinition.MOD; break;
                case BinaryOperationType.Exponentiation: def = InstructionDefinition.POW; break;
                case BinaryOperationType.ShiftLeft: def = InstructionDefinition.SL; break;
                case BinaryOperationType.ShiftRight: def = InstructionDefinition.SR; break;
                case BinaryOperationType.RotateLeft: def = InstructionDefinition.RL; break;
                case BinaryOperationType.RotateRight: def = InstructionDefinition.RR; break;
                case BinaryOperationType.And: def = InstructionDefinition.AND; break;
                case BinaryOperationType.Or: def = InstructionDefinition.OR; break;
                case BinaryOperationType.Xor: def = InstructionDefinition.XOR; break;
                case BinaryOperationType.NotAnd: def = InstructionDefinition.NAND; break;
                case BinaryOperationType.NotOr: def = InstructionDefinition.NOR; break;
                case BinaryOperationType.NotXor: def = InstructionDefinition.NXOR; break;
                case BinaryOperationType.Equals: def = InstructionDefinition.EQ; break;
                case BinaryOperationType.NotEquals: def = InstructionDefinition.NEQ; break;
                case BinaryOperationType.LessThan: def = InstructionDefinition.LT; break;
                case BinaryOperationType.LessThanOrEqual: def = InstructionDefinition.LTE; break;
                case BinaryOperationType.GreaterThan: def = InstructionDefinition.GT; break;
                case BinaryOperationType.GreaterThanOrEqual: def = InstructionDefinition.GTE; break;
                default: Debug.Assert(false); break;
            }

            this.Emit(def, this.GetParameter(n.Target), this.GetParameter(n.Left), this.GetParameter(n.Right));
        }

        private void Visit(ReturnTerminator r) {
            this.Emit(InstructionDefinition.SET, Function.R0Param, this.GetParameter(r.Value));
            this.Emit(InstructionDefinition.RET);
        }

        private void Visit(CallTerminator r) {
            this.Emit(InstructionDefinition.SET, Function.StackParam, Function.RbpParam);

            foreach (var a in r.Arguments)
                this.Emit(InstructionDefinition.SET, Function.StackParam, this.GetParameter(a));

            this.Emit(InstructionDefinition.SUB, Function.RbpParam, Function.RspParam, Parameter.CreateLiteral((ulong)r.Arguments.Count));

            this.Emit(InstructionDefinition.ADD, Function.RspParam, Function.RspParam, Parameter.CreateLiteral((ulong)r.Target.LocalVariables.Count));

            this.EmitCall(r.Target);

            this.Emit(InstructionDefinition.SUB, Function.RspParam, Function.RspParam, Parameter.CreateLiteral((ulong)(r.Arguments.Count + r.Target.LocalVariables.Count)));

            this.Emit(InstructionDefinition.SET, Function.RbpParam, Function.StackParam);

            this.Emit(InstructionDefinition.SET, this.GetParameter(r.Result), Function.R0Param);

            this.EmitJump(r.Next);
        }

        private void Visit(IfTerminator i) {
            this.EmitJump(i.NextFalse, i.Condition);
            this.EmitJump(i.NextTrue);
        }

        private void Visit(GotoTerminator g) => this.EmitJump(g.Next);

        private void EmitJump(BasicBlock target) => this.EmitJump(target, null);

        private void EmitJump(BasicBlock target, RValue whenZero) {
            var param = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);

            if (whenZero == null) {
                this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), param);
            }
            else {
                this.Emit(InstructionDefinition.SET, this.GetParameter(whenZero), InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), param);
            }

            this.jumpFixups.Add((param, target));
        }

        private void EmitCall(FunctionSymbol target) {
            var param = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);

            this.Emit(InstructionDefinition.CALL, param);

            this.callFixups.Add((param, target));
        }
    }
}
