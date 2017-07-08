using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class Emitter {
        private static Parameter StackParam { get; } = new Parameter { Type = ParameterType.Stack };

        private readonly Compiliation tree;
        private readonly bool emitAssemblyListing;
        private readonly bool emitBootable;
        private readonly string outputFile;
        private ulong nextGlobalVariableAddress;
        private HashSet<BasicBlock> visitedBasicBlocks;
        private Dictionary<BasicBlock, ulong> basicBlockAddresses;
        private Dictionary<FunctionSymbol, ulong> functionAddresses;
        private Dictionary<GlobalVariableSymbol, ulong> globalVariableAddresses;
        private List<Instruction> instructions;
        private FunctionSymbol currentFunction;
        private bool throwOnNoFunction;

        public Emitter(Compiliation tree, bool emitAssemblyListing, bool emitBootable, string outputFile) => (this.tree, this.emitAssemblyListing, this.emitBootable, this.outputFile) = (tree, emitAssemblyListing, emitBootable, outputFile);

        private ulong DistanceFrom(int startInst) => (ulong)this.instructions.Skip(startInst).Sum(i => i.Length);

        public void Emit() {
            this.basicBlockAddresses = new Dictionary<BasicBlock, ulong>();
            this.functionAddresses = new Dictionary<FunctionSymbol, ulong>();
            this.globalVariableAddresses = new Dictionary<GlobalVariableSymbol, ulong>();
            this.nextGlobalVariableAddress = 0UL;

            this.visitedBasicBlocks = new HashSet<BasicBlock>();
            this.instructions = new List<Instruction>();
            this.throwOnNoFunction = false;

            this.EmitHeader();
            this.DiscoverAddresses(this.tree);

            this.visitedBasicBlocks.Clear();
            this.instructions.Clear();
            this.throwOnNoFunction = true;

            this.EmitHeader();
            this.Visit(this.tree);

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    if (this.emitBootable)
                        writer.Write(0x00000000454B5241UL);

                    if (this.emitAssemblyListing)
                        File.WriteAllLines(Path.ChangeExtension(this.outputFile, "lst"), this.globalVariableAddresses.Select(i => $"{i.Key}: 0x{i.Value:X16}").Concat(this.instructions.Select(i => i.ToString())));

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(this.outputFile, stream.ToArray());
                }
            }
        }

        private void EmitHeader() {
            var entry = this.tree.Functions.Single(f => f.Symbol.Name == "main");

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)entry.Symbol.LocalVariables.Count + 0x1_0000));
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Parameter.CreateLiteral(0x1_0000));
        }

        private void SetGlobalVariableAddress(GlobalVariableSymbol v) {
            if (this.globalVariableAddresses.ContainsKey(v))
                throw new AlreadyDefinedException(default(PositionInfo), v.Name);

            this.globalVariableAddresses[v] = this.nextGlobalVariableAddress++;
        }

        private void DiscoverAddresses(Compiliation n) {
            foreach (var s in n.GlobalVariables)
                this.SetGlobalVariableAddress(s);

            foreach (var s in n.Functions) {
                if (this.functionAddresses.ContainsKey(s.Symbol))
                    throw new AlreadyDefinedException(default(PositionInfo), s.Symbol.Name);

                this.functionAddresses[s.Symbol] = (ulong)this.instructions.Sum(i => i.Length);

                this.Visit(s);
            }
        }

        private Parameter GetVariableAccessParameter(RValue variable, bool allowIndirect) {
            switch (variable) {
                case UnsignedIntegerConstant v:
                    return Parameter.CreateLiteral(v.Value);

                case LValue v:
                    return this.GetVariableAccessParameter(v, allowIndirect);

                default:
                    Debug.Assert(false);

                    return null;
            }
        }

        private Parameter GetVariableAccessParameter(LValue variable, bool allowIndirect) {
            var addr = 0UL;

            switch (variable) {
                case LocalVariableLValue v:
                    var vidx = 0UL;
                    var localArg = this.currentFunction.LocalVariables.SkipWhile(a => { vidx++; return a != v.Symbol; }).FirstOrDefault();

                    if (localArg == null)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral((vidx + (ulong)this.currentFunction.Arguments.Count) - 1, ParameterFlags.RelativeToRBP | (allowIndirect ? ParameterFlags.Indirect : 0));

                case GlobalVariableLValue v:
                    if (!this.globalVariableAddresses.TryGetValue(v.Symbol, out addr) && this.throwOnNoFunction)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral(addr, allowIndirect ? ParameterFlags.Indirect : 0);

                case ArgumentLValue v:
                    var aidx = 0UL;
                    var arg = this.currentFunction.Arguments.SkipWhile(a => { aidx++; return a != v.Symbol; }).FirstOrDefault();

                    if (arg == null)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Symbol.Name);

                    return Parameter.CreateLiteral(aidx - 1, ParameterFlags.RelativeToRBP | (allowIndirect ? ParameterFlags.Indirect : 0));

                case RegisterLValue v:
                    return Parameter.CreateRegister(v.Symbol.Register);

                case PointerLValue v:
                    var r = this.GetVariableAccessParameter(v.Target, false);

                    r.IsIndirect = true;

                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, r);

                    return Parameter.CreateStack(ParameterFlags.Indirect);

                default:
                    Debug.Assert(false);

                    throw new InvalidOperationException();
            }
        }

        private Parameter GetFunctionAccessParameter(FunctionSymbol func) {
            if (!this.functionAddresses.TryGetValue(func, out var addr) && this.throwOnNoFunction)
                throw new IdentifierNotFoundException(default(PositionInfo), func.Name);

            return Parameter.CreateLiteral(addr - this.DistanceFrom(0), ParameterFlags.RelativeToRIP);
        }

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void Visit(Compiliation n) {
            foreach (var s in n.Functions)
                this.Visit(s);
        }

        private void Visit(Function n) {
            this.currentFunction = n.Symbol;

            this.Visit(n.Entry);
        }

        private void Visit(BasicBlock n) {
            if (this.visitedBasicBlocks.Contains(n))
                return;

            this.visitedBasicBlocks.Add(n);

            if (!this.throwOnNoFunction) {
                if (this.basicBlockAddresses.ContainsKey(n))
                    return;

                this.basicBlockAddresses[n] = (ulong)this.instructions.Sum(i => i.Length);
            }

            foreach (var s in n.Instructions)
                this.Visit(s);

            this.Visit(n.Terminator);
        }

        private void Visit(BasicBlockInstruction s) {
            switch (s) {
                case BasicBlockAssignmentInstruction n: this.Visit(n); break;
                case BasicBlockBinaryOperationInstruction n: this.Visit(n); break;
                case BasicBlockAddressOfInstruction n: this.Emit(InstructionDefinition.SET, this.GetVariableAccessParameter(n.Target, true), this.GetVariableAccessParameter(n.Value, false)); break;
                case BasicBlockIntrinsicInstruction n: this.Visit(n); break;
                default: Debug.Assert(false); break;
            }
        }

        private void Visit(BasicBlockIntrinsicInstruction s) {
            var a = s.Argument1 != null ? this.GetVariableAccessParameter(s.Argument1, true) : null;
            var b = s.Argument2 != null ? this.GetVariableAccessParameter(s.Argument2, true) : null;
            var c = s.Argument3 != null ? this.GetVariableAccessParameter(s.Argument3, true) : null;

            this.Emit(s.Intrinsic, a, b, c);
        }

        private void Visit(Terminator terminator) {
            switch (terminator) {
                case ReturnTerminator n: this.Visit(n); break;
                case CallTerminator n: this.Visit(n); break;
                case IfTerminator n: this.Visit(n); break;
                case GotoTerminator n: this.Visit(n); break;
                default: Debug.Assert(false); break;
            }
        }

        private void Visit(ReturnTerminator r) {
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.R0), this.GetVariableAccessParameter(r.Value, true));
            this.Emit(InstructionDefinition.RET);
        }

        private void Visit(CallTerminator r) {
            var orig = this.currentFunction;

            this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(Register.RBP));

            foreach (var a in r.Arguments)
                this.Emit(InstructionDefinition.SET, Emitter.StackParam, this.GetVariableAccessParameter(a, true));

            this.currentFunction = r.Target;

            this.Emit(InstructionDefinition.SUB, Parameter.CreateRegister(Register.RBP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)r.Arguments.Count));

            this.Emit(InstructionDefinition.ADD, Parameter.CreateRegister(Register.RSP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)this.currentFunction.LocalVariables.Count));

            this.Emit(InstructionDefinition.CALL, this.GetFunctionAccessParameter(r.Target));

            this.Emit(InstructionDefinition.SUB, Parameter.CreateRegister(Register.RSP), Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral((ulong)(r.Arguments.Count + this.currentFunction.LocalVariables.Count)));

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Emitter.StackParam);

            this.currentFunction = orig;

            this.Emit(InstructionDefinition.SET, this.GetVariableAccessParameter(r.Result, true), Parameter.CreateRegister(Register.R0));

            this.Visit(r.Next);
        }

        private void Visit(IfTerminator i) {
            var ifStart = this.instructions.Count;
            var ifLen = Parameter.CreateLiteral(0, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, this.GetVariableAccessParameter(i.Condition, true), InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), ifLen);

            this.Visit(i.NextTrue);

            var elseStart = this.instructions.Count;
            var elseLen = Parameter.CreateLiteral(0, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), elseLen);

            ifLen.Literal = this.DistanceFrom(ifStart);

            this.Visit(i.NextFalse);

            elseLen.Literal = this.DistanceFrom(elseStart);
        }

        private void Visit(GotoTerminator g) {
            this.Visit(g.Next);

            var targetAddr = this.throwOnNoFunction ? this.basicBlockAddresses[g.Next] : 0;
            var currentAddr = this.throwOnNoFunction ? this.DistanceFrom(0) : 0;

            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), Parameter.CreateLiteral(targetAddr - currentAddr, ParameterFlags.RelativeToRIP));
        }

        private void Visit(BasicBlockAssignmentInstruction a) {
            var target = this.GetVariableAccessParameter(a.Target, true);

            if (a.Value is LValue vnode) {
                this.Emit(InstructionDefinition.SET, target, this.GetVariableAccessParameter(vnode, true));
            }
            else if (a.Value is UnsignedIntegerConstant nnode) {
                this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral(nnode.Value));
            }
            else {
                Debug.Assert(false);
            }
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

            this.Emit(def, this.GetVariableAccessParameter(n.Target, true), this.GetVariableAccessParameter(n.Left, true), this.GetVariableAccessParameter(n.Right, true));
        }
    }
}
