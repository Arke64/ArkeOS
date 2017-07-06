using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
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
        private ulong nextVariableAddress;
        private HashSet<BasicBlock> visitedBasicBlocks;
        private Dictionary<BasicBlock, ulong> basicBlockAddresses;
        private Dictionary<string, ulong> functionAddresses;
        private Dictionary<string, ulong> variableAddresses;
        private List<Instruction> instructions;
        private Function currentFunction;
        private bool throwOnNoFunction;

        public Emitter(Compiliation tree, bool emitAssemblyListing, bool emitBootable, string outputFile) => (this.tree, this.emitAssemblyListing, this.emitBootable, this.outputFile) = (tree, emitAssemblyListing, emitBootable, outputFile);

        private ulong DistanceFrom(int startInst) => (ulong)this.instructions.Skip(startInst).Sum(i => i.Length);

        public void Emit() {
            this.basicBlockAddresses = new Dictionary<BasicBlock, ulong>();
            this.functionAddresses = new Dictionary<string, ulong>();
            this.variableAddresses = new Dictionary<string, ulong>();
            this.nextVariableAddress = 0UL;

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
                        File.WriteAllLines(Path.ChangeExtension(this.outputFile, "lst"), this.variableAddresses.Select(i => $"{i.Key}: 0x{i.Value:X16}").Concat(this.instructions.Select(i => i.ToString())));

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(this.outputFile, stream.ToArray());
                }
            }
        }

        private void EmitHeader() {
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral(0x1_0000));
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RBP), Parameter.CreateRegister(Register.RSP));
            this.Emit(InstructionDefinition.CALL, this.GetFunctionAccessParameter("main"));
            this.Emit(InstructionDefinition.HLT);
        }

        private void SetVariableAddress(GlobalVariableLValue v) {
            if (this.variableAddresses.ContainsKey(v.Identifier))
                throw new AlreadyDefinedException(default(PositionInfo), v.Identifier);

            this.variableAddresses[v.Identifier] = this.nextVariableAddress++;
        }

        private void DiscoverAddresses(Compiliation n) {
            foreach (var s in n.GlobalVariables)
                this.SetVariableAddress(s);

            foreach (var s in n.Functions) {
                if (this.functionAddresses.ContainsKey(s.Identifier))
                    throw new AlreadyDefinedException(default(PositionInfo), s.Identifier);

                this.functionAddresses[s.Identifier] = (ulong)this.instructions.Sum(i => i.Length);

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
                    var idx = 0UL;
                    var arg = this.currentFunction.Arguments.SkipWhile(a => { idx++; return a != v.Identifier; }).FirstOrDefault();

                    if (arg != null)
                        return Parameter.CreateLiteral(idx - 1, ParameterFlags.RelativeToRBP | (allowIndirect ? ParameterFlags.Indirect : 0));

                    idx = 0UL;
                    var localArg = this.currentFunction.LocalVariables.SkipWhile(a => { idx++; return a.Identifier != v.Identifier; }).FirstOrDefault();

                    if (localArg != null)
                        return Parameter.CreateLiteral((idx + (ulong)this.currentFunction.Arguments.Count) - 1, ParameterFlags.RelativeToRBP | (allowIndirect ? ParameterFlags.Indirect : 0));

                    if (this.tree.Consts.TryGetValue(v.Identifier, out var c))
                        return Parameter.CreateLiteral(c.Value);

                    if (!this.variableAddresses.TryGetValue(v.Identifier, out addr) && this.throwOnNoFunction)
                        throw new IdentifierNotFoundException(default(PositionInfo), v.Identifier);

                    break;

                case RegisterLValue v:
                    return Parameter.CreateRegister(v.Register);

                case PointerLValue v:
                    var r = this.GetVariableAccessParameter(v.Reference, false);

                    r.IsIndirect = true;

                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, r);

                    return Parameter.CreateStack(ParameterFlags.Indirect);

                default:
                    Debug.Assert(false);

                    break;
            }

            return Parameter.CreateLiteral(addr, allowIndirect ? ParameterFlags.Indirect : 0);
        }

        private Parameter GetFunctionAccessParameter(string func) {
            if (!this.functionAddresses.TryGetValue(func, out var addr) && this.throwOnNoFunction)
                throw new IdentifierNotFoundException(default(PositionInfo), func);

            return Parameter.CreateLiteral(addr - this.DistanceFrom(0), ParameterFlags.RelativeToRIP);
        }

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void Visit(Compiliation n) {
            foreach (var s in n.Functions)
                this.Visit(s);
        }

        private void Visit(Function n) {
            this.currentFunction = n;

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
                case BasicBlockUnaryOperationInstruction n: this.Visit(n); break;
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

            this.currentFunction = this.tree.Functions.Single(f => f.Identifier == r.Target);

            this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(Register.RBP));

            foreach (var a in r.Arguments)
                this.Emit(InstructionDefinition.SET, Emitter.StackParam, this.GetVariableAccessParameter(a, true));

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

        private void Visit(BasicBlockUnaryOperationInstruction n) {
            var target = this.GetVariableAccessParameter(n.Target, true);

            if (n.Op == UnaryOperationType.AddressOf) {
                this.Emit(InstructionDefinition.SET, target, this.GetVariableAccessParameter(n.Value, false));
            }
            else {
                switch (n.Op) {
                    case UnaryOperationType.Minus: this.Emit(InstructionDefinition.MUL, target, this.GetVariableAccessParameter(n.Value, true), Parameter.CreateLiteral(ulong.MaxValue)); break;
                    case UnaryOperationType.Not: this.Emit(InstructionDefinition.NOT, target, this.GetVariableAccessParameter(n.Value, true)); break;
                    case UnaryOperationType.Dereference: this.Emit(InstructionDefinition.SET, target, this.GetVariableAccessParameter(n.Value, true)); break;
                    case UnaryOperationType.AddressOf: throw new ExpectedException(default(PositionInfo), "identifier");
                    default: Debug.Assert(false); break;
                }
            }
        }
    }
}
