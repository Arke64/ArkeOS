using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

                    if (this.options.EmitAssemblyListing)
                        File.WriteAllLines(Path.ChangeExtension(this.options.OutputName, "lst"), this.variableOffsets.Select(i => $"{i.Key.Name}: 0x{i.Value:X16}").Concat(this.instructions.Select(i => i.ToString())));

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
        private static Parameter StackParam { get; } = new Parameter { Type = ParameterType.Stack };

        private readonly Dictionary<BasicBlock, ulong> blockOffsets = new Dictionary<BasicBlock, ulong>();

        private readonly Dictionary<Parameter, BasicBlock> jumpFixups = new Dictionary<Parameter, BasicBlock>();
        private readonly Dictionary<Parameter, FunctionSymbol> callFixups = new Dictionary<Parameter, FunctionSymbol>();

        private readonly List<Instruction> instructions = new List<Instruction>();

        private readonly IReadOnlyDictionary<GlobalVariableSymbol, ulong> variableOffsets;
        private readonly IR.Function source;
        private readonly FunctionSymbol currentFunction;

        private ulong length = 0;

        private void Add(Instruction i) {
            this.instructions.Add(i);
            this.length += i.Length;
        }

        public IReadOnlyCollection<Instruction> Instructions => this.instructions;
        public ulong Length => this.length;

        public Function(IR.Function source, IReadOnlyDictionary<GlobalVariableSymbol, ulong> globalVariables) => (this.source, this.variableOffsets, this.currentFunction) = (source, globalVariables, source.Symbol);

        public void Emit() {
            foreach (var f in this.source.AllBlocks)
                this.Visit(f);
        }

        public void Fixup(IReadOnlyDictionary<FunctionSymbol, ulong> functionOffsets) {
            var start = functionOffsets[this.currentFunction];

            foreach (var f in this.jumpFixups)
                f.Key.Literal = this.blockOffsets[f.Value] - f.Key.Literal;

            foreach (var f in this.callFixups)
                f.Key.Literal = functionOffsets[f.Value] - (start + f.Key.Literal);
        }

        private ulong CurrentOffset => (ulong)this.instructions.Sum(i => i.Length);

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void Visit(BasicBlock node) {
            if (node.Instructions.Count() == 0 && node.Terminator == null) return;

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

        private Parameter GetParameter(FunctionSymbol variable) {
            var param = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);

            this.callFixups[param] = variable;

            return param;
        }

        private void Visit(BasicBlockIntrinsicInstruction s) {
            var a = s.Argument1 != null ? this.GetParameter(s.Argument1) : null;
            var b = s.Argument2 != null ? this.GetParameter(s.Argument2) : null;
            var c = s.Argument3 != null ? this.GetParameter(s.Argument3) : null;

            this.Emit(s.Intrinsic, a, b, c);
        }

        private void Visit(BasicBlockAssignmentInstruction a) {
            this.Emit(InstructionDefinition.SET, this.GetParameter(a.Target), this.GetParameter(a.Value));
        }

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
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.R0), this.GetParameter(r.Value));
            this.Emit(InstructionDefinition.RET);
        }

        private void Visit(CallTerminator r) {
            this.Emit(InstructionDefinition.SET, Function.StackParam, Parameter.CreateRegister(Register.RBP));

            foreach (var a in r.Arguments)
                this.Emit(InstructionDefinition.SET, Function.StackParam, this.GetParameter(a));

            //TODO Cache these registers
            this.Emit(InstructionDefinition.SUB, Parameter.CreateRegister(Register.RBP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)r.Arguments.Count));

            this.Emit(InstructionDefinition.ADD, Parameter.CreateRegister(Register.RSP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)r.Target.LocalVariables.Count));

            this.Emit(InstructionDefinition.CALL, this.GetParameter(r.Target));

            this.Emit(InstructionDefinition.SUB, Parameter.CreateRegister(Register.RSP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)(r.Arguments.Count + r.Target.LocalVariables.Count)));

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Function.StackParam);

            this.Emit(InstructionDefinition.SET, this.GetParameter(r.Result), Parameter.CreateRegister(Register.R0));

            var param = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), param);

            this.jumpFixups.Add(param, r.Next);
        }

        private void Visit(IfTerminator i) {
            var param2 = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, this.GetParameter(i.Condition), InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), param2);

            var param1 = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), param1);

            this.jumpFixups.Add(param1, i.NextTrue);
            this.jumpFixups.Add(param2, i.NextFalse);
        }

        private void Visit(GotoTerminator g) {
            var param = Parameter.CreateLiteral(this.CurrentOffset, ParameterFlags.RelativeToRIP);

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), param);

            this.jumpFixups.Add(param, g.Next);
        }
    }
}
