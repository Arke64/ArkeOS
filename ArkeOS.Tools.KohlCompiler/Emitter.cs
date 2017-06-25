using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class Emitter {
        private static Parameter StackParam { get; } = new Parameter { Type = ParameterType.Stack };

        private readonly ProgramNode tree;
        private List<Instruction> instructions;

        public Emitter(ProgramNode tree) => this.tree = tree;

        public void Emit(string outputFile) {
            this.instructions = new List<Instruction>();

            this.Visit(this.tree);

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(0x00000000454B5241UL);

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(outputFile, stream.ToArray());
                }
            }
        }

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters, conditional, conditionalType));

        private void EmitConditional(StatementBlockNode block) {
            var start = this.instructions.Count;
            var len = Parameter.CreateLiteral(0, ParameterFlags.RIPRelative);

            this.Emit(InstructionDefinition.SET, Emitter.StackParam, InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), len);

            this.Visit(block);

            len.Literal = (ulong)this.instructions.Skip(start).Sum(i => i.Length);
        }

        private Parameter ExtractLValue(ExpressionNode expr) {
            if (expr is LValueNode lvalue) {
                switch (lvalue) {
                    case RegisterNode n: return Parameter.CreateRegister(n.Register);
                    default: throw new NotImplementedException();
                }
            }

            throw new ExpectedException(default(PositionInfo), "value");
        }

        private void Visit(ProgramNode n) => this.Visit(n.StatementBlock);

        private void Visit(StatementBlockNode n) {
            foreach (var s in n.Statements)
                this.Visit(s);
        }

        private void Visit(StatementNode s) {
            switch (s) {
                case IfStatementNode n:
                    this.Visit(n.Expression);

                    this.EmitConditional(n.StatementBlock);

                    break;

                case CompoundAssignmentStatementNode n: {
                        var target = this.ExtractLValue(n.Target);

                        this.Visit(new BinaryExpressionNode(n.Target, n.Op, n.Expression));

                        this.Emit(InstructionDefinition.SET, target, Emitter.StackParam);
                    }

                    break;

                case AssignmentStatementNode n: {
                        var target = this.ExtractLValue(n.Target);

                        if (n.Expression is RegisterNode rnode) {
                            this.Emit(InstructionDefinition.SET, target, Parameter.CreateRegister(rnode.Register));
                        }
                        else if (n.Expression is IntegerLiteralNode nnode) {
                            this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral((ulong)nnode.Literal));
                        }
                        else if (n.Expression is BoolLiteralNode bnode) {
                            this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral(bnode.Literal ? ulong.MaxValue : 0));
                        }
                        else {
                            this.Visit(n.Expression);

                            this.Emit(InstructionDefinition.SET, target, Emitter.StackParam);
                        }
                    }

                    break;

                case BrkStatementNode n: this.Emit(InstructionDefinition.BRK); break;
                case EintStatementNode n: this.Emit(InstructionDefinition.EINT); break;
                case HltStatementNode n: this.Emit(InstructionDefinition.HLT); break;
                case IntdStatementNode n: this.Emit(InstructionDefinition.INTD); break;
                case InteStatementNode n: this.Emit(InstructionDefinition.INTE); break;
                case NopStatementNode n: this.Emit(InstructionDefinition.NOP); break;

                case CpyStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            this.Visit(arg0);
                            this.Visit(arg1);
                            this.Visit(arg2);

                            this.Emit(InstructionDefinition.CPY, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam);
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case IntStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            this.Visit(arg0);
                            this.Visit(arg1);
                            this.Visit(arg2);

                            this.Emit(InstructionDefinition.INT, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam);
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case DbgStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            this.Emit(InstructionDefinition.DBG, this.ExtractLValue(arg0), this.ExtractLValue(arg1), this.ExtractLValue(arg2));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case CasStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            this.Visit(arg2);
                            this.Emit(InstructionDefinition.CAS, this.ExtractLValue(arg0), this.ExtractLValue(arg1), Emitter.StackParam);
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case XchgStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1)) {
                            this.Emit(InstructionDefinition.XCHG, this.ExtractLValue(arg0), this.ExtractLValue(arg1));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(ExpressionNode e) {
            switch (e) {
                case BinaryExpressionNode n:
                    this.Visit(n.Left);
                    this.Visit(n.Right);

                    var def = default(InstructionDefinition);

                    switch (n.Op.Operator) {
                        case Operator.Addition: def = InstructionDefinition.ADD; break;
                        case Operator.Subtraction: def = InstructionDefinition.SUB; break;
                        case Operator.Multiplication: def = InstructionDefinition.MUL; break;
                        case Operator.Division: def = InstructionDefinition.DIV; break;
                        case Operator.Remainder: def = InstructionDefinition.MOD; break;
                        case Operator.Exponentiation: def = InstructionDefinition.POW; break;
                        case Operator.ShiftLeft: def = InstructionDefinition.SL; break;
                        case Operator.ShiftRight: def = InstructionDefinition.SR; break;
                        case Operator.RotateLeft: def = InstructionDefinition.RL; break;
                        case Operator.RotateRight: def = InstructionDefinition.RR; break;
                        case Operator.And: def = InstructionDefinition.AND; break;
                        case Operator.Or: def = InstructionDefinition.OR; break;
                        case Operator.Xor: def = InstructionDefinition.XOR; break;
                        case Operator.NotAnd: def = InstructionDefinition.NAND; break;
                        case Operator.NotOr: def = InstructionDefinition.NOR; break;
                        case Operator.NotXor: def = InstructionDefinition.NXOR; break;
                        case Operator.Equals: def = InstructionDefinition.EQ; break;
                        case Operator.NotEquals: def = InstructionDefinition.NEQ; break;
                        case Operator.LessThan: def = InstructionDefinition.LT; break;
                        case Operator.LessThanOrEqual: def = InstructionDefinition.LTE; break;
                        case Operator.GreaterThan: def = InstructionDefinition.GT; break;
                        case Operator.GreaterThanOrEqual: def = InstructionDefinition.GTE; break;

                        default: Debug.Assert(false); break;
                    }

                    this.Emit(def, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam);

                    break;

                case UnaryExpressionNode n:
                    this.Visit(n.Expression);

                    switch (n.Op.Operator) {
                        case Operator.UnaryPlus: break;
                        case Operator.UnaryMinus: this.Emit(InstructionDefinition.MUL, Emitter.StackParam, Emitter.StackParam, Parameter.CreateLiteral(ulong.MaxValue)); break;
                        case Operator.Not: this.Emit(InstructionDefinition.NOT, Emitter.StackParam, Emitter.StackParam); break;
                        default: Debug.Assert(false); break;
                    }

                    break;

                case RValueNode n:
                    this.Visit(n);

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(RValueNode rvalue) {
            switch (rvalue) {
                case IntegerLiteralNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral((ulong)n.Literal));

                    break;

                case BoolLiteralNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral(n.Literal ? ulong.MaxValue : 0));
                    break;

                case LValueNode n:
                    this.Visit(n);

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(LValueNode lvalue) {
            switch (lvalue) {
                case RegisterNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(n.Register));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
