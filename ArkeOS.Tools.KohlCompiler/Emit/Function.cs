using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArkeOS.Tools.KohlCompiler.Emit {
    public sealed class Function {
        private static Parameter StackParam { get; } = Parameter.CreateStack();
        private static Parameter RbpParam { get; } = Parameter.CreateRegister(Register.RBP);
        private static Parameter RspParam { get; } = Parameter.CreateRegister(Register.RSP);
        private static Parameter R0Param { get; } = Parameter.CreateRegister(Register.R0);

        private readonly Dictionary<BasicBlock, ulong> blockOffsets = new Dictionary<BasicBlock, ulong>();
        private readonly List<(Parameter src, BasicBlock target)> jumpFixups = new List<(Parameter, BasicBlock)>();
        private readonly List<(Parameter src, FunctionSymbol target)> callFixups = new List<(Parameter, FunctionSymbol)>();
        private readonly List<(Parameter src, GlobalVariableSymbol target)> globalVariableFixups = new List<(Parameter, GlobalVariableSymbol)>();
        private readonly List<Instruction> instructions = new List<Instruction>();
        private ulong currentOffset = 0;

        public IReadOnlyCollection<Instruction> Instructions => this.instructions;
        public ulong Length => this.currentOffset;
        public IR.Function Source { get; }

        public Function(IR.Function source) => this.Source = source;

        public void Emit() {
            foreach (var node in this.Source.AllBlocks) {
                this.blockOffsets[node] = this.currentOffset;

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
        }

        public void Fixup(IReadOnlyDictionary<FunctionSymbol, ulong> functionAddresses, IReadOnlyDictionary<GlobalVariableSymbol, ulong> globalVariableAddresses) {
            var thisFileOffset = functionAddresses[this.Source.Symbol];

            foreach (var f in this.globalVariableFixups)
                f.src.Literal += globalVariableAddresses.TryGetValue(f.target, out var addr) ? addr : throw new IdentifierNotFoundException(default(PositionInfo), f.target.Name);

            foreach (var f in this.jumpFixups)
                f.src.Literal = this.blockOffsets[f.target] - f.src.Literal;

            foreach (var f in this.callFixups)
                f.src.Literal = functionAddresses[f.target] - (thisFileOffset + f.src.Literal);
        }

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void Add(Instruction i) {
            this.instructions.Add(i);
            this.currentOffset += i.Length;
        }

        private Parameter GetParameter(RValue variable) {
            switch (variable) {
                case IntegerRValue v: return Parameter.CreateLiteral(v.Value);
                case AddressOfRValue n: var res = this.GetParameter(n.Target); res.IsIndirect = false; return res;
                case LValue v: return this.GetParameter(v);
                default: Debug.Assert(false); throw new InvalidOperationException();
            }
        }

        private Parameter GetParameter(LValue variable) {
            switch (variable) {
                case RegisterLValue v: return Parameter.CreateRegister(v.Symbol.Register);
                case ArgumentLValue v: return Parameter.CreateLiteral(this.Source.Symbol.GetStackPosition(v.Symbol), ParameterFlags.RelativeToRBP | ParameterFlags.Indirect);
                case LocalVariableLValue v: return Parameter.CreateLiteral(this.Source.Symbol.GetStackPosition(v.Symbol), ParameterFlags.RelativeToRBP | ParameterFlags.Indirect);
                case GlobalVariableLValue v: return this.EmitGlobalVariable(v.Symbol);
                case PointerLValue v: return this.Dereference(v);
                case StructMemberLValue v:
                    var b = this.GetParameter(v.Target);

                    this.Emit(InstructionDefinition.SET, Function.StackParam, b);
                    this.Emit(InstructionDefinition.ADD, Function.StackParam, Function.StackParam, Parameter.CreateLiteral(v.Member.Offset));

                    return Parameter.CreateStack(ParameterFlags.Indirect);

                default: Debug.Assert(false); throw new InvalidOperationException();
            }
        }

        private Parameter Dereference(PointerLValue value) {
            this.Emit(InstructionDefinition.SET, Function.StackParam, this.GetParameter(value.Target));

            return Parameter.CreateStack(ParameterFlags.Indirect);
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

            //TODO Need to increment by the actual size
            this.Emit(InstructionDefinition.SUB, Function.RbpParam, Function.RspParam, Parameter.CreateLiteral((ulong)r.Arguments.Count));

            this.Emit(InstructionDefinition.ADD, Function.RspParam, Function.RspParam, Parameter.CreateLiteral((ulong)r.Target.LocalVariables.Count));

            this.EmitCall(r.Target);

            this.Emit(InstructionDefinition.SUB, Function.RspParam, Function.RspParam, Parameter.CreateLiteral(r.Target.StackRequired));

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
            var param = Parameter.CreateLiteral(this.currentOffset, ParameterFlags.RelativeToRIP);

            if (whenZero == null) {
                this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), param);
            }
            else {
                this.Emit(InstructionDefinition.SET, this.GetParameter(whenZero), InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), param);
            }

            this.jumpFixups.Add((param, target));
        }

        private void EmitCall(FunctionSymbol target) {
            var param = Parameter.CreateLiteral(this.currentOffset, ParameterFlags.RelativeToRIP);

            this.Emit(InstructionDefinition.CALL, param);

            this.callFixups.Add((param, target));
        }

        private Parameter EmitGlobalVariable(GlobalVariableSymbol target) {
            var param = Parameter.CreateLiteral(0, ParameterFlags.Indirect);

            this.globalVariableFixups.Add((param, target));

            return param;
        }
    }
}
