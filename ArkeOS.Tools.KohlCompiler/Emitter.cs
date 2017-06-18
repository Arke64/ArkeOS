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
            if (expr is IdentifierNode i) {
                switch (i) {
                    case RegisterNode n: return Parameter.CreateRegister(n.Register);
                    default: throw new NotImplementedException();
                }
            }

            throw new ExceptedLValueException();
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

                case AssignmentStatementNode n:
                    var target = this.ExtractLValue(n.Target);

                    if (n.Expression is RegisterNode rnode) {
                        this.Emit(InstructionDefinition.SET, target, Parameter.CreateRegister(rnode.Register));
                    }
                    else if (n.Expression is NumberNode nnode) {
                        this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral((ulong)nnode.Number));
                    }
                    else {
                        this.Visit(n.Expression);

                        this.Emit(InstructionDefinition.SET, target, Emitter.StackParam);
                    }

                    break;

                case BrkStatementNode n: this.Emit(InstructionDefinition.BRK); break;
                case EintStatementNode n: this.Emit(InstructionDefinition.EINT); break;
                case HltStatementNode n: this.Emit(InstructionDefinition.HLT); break;
                case IntdStatementNode n: this.Emit(InstructionDefinition.INTD); break;
                case InteStatementNode n: this.Emit(InstructionDefinition.INTE); break;
                case NopStatementNode n: this.Emit(InstructionDefinition.NOP); break;
                case CpyStatementNode n: this.Visit(n.ArgumentList); this.Emit(InstructionDefinition.CPY, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                case IntStatementNode n: this.Visit(n.ArgumentList); this.Emit(InstructionDefinition.INT, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;

                case DbgStatementNode n: {
                        if (n.ArgumentList.Extract(0, out var arg0) && n.ArgumentList.Extract(1, out var arg1) && n.ArgumentList.Extract(2, out var arg2)) {
                            this.Emit(InstructionDefinition.DBG, this.ExtractLValue(arg0), this.ExtractLValue(arg1), this.ExtractLValue(arg2));
                        }
                        else {
                            throw new TooFewArgumentsException();
                        }
                    }

                    break;

                case CasStatementNode n: {
                        if (n.ArgumentList.Extract(0, out var arg0) && n.ArgumentList.Extract(1, out var arg1) && n.ArgumentList.Extract(2, out var arg2)) {
                            this.Visit(arg2);
                            this.Emit(InstructionDefinition.CAS, this.ExtractLValue(arg0), this.ExtractLValue(arg1), Emitter.StackParam);
                        }
                        else {
                            throw new TooFewArgumentsException();
                        }
                    }

                    break;

                case XchgStatementNode n: {
                        if (n.ArgumentList.Extract(0, out var arg0) && n.ArgumentList.Extract(1, out var arg1)) {
                            this.Emit(InstructionDefinition.XCHG, this.ExtractLValue(arg0), this.ExtractLValue(arg1));
                        }
                        else {
                            throw new TooFewArgumentsException();
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

                    switch (n.Op.Operator) {
                        case Operator.Addition: this.Emit(InstructionDefinition.ADD, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        case Operator.Subtraction: this.Emit(InstructionDefinition.SUB, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        case Operator.Multiplication: this.Emit(InstructionDefinition.MUL, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        case Operator.Division: this.Emit(InstructionDefinition.DIV, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        case Operator.Exponentiation: this.Emit(InstructionDefinition.POW, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        case Operator.Remainder: this.Emit(InstructionDefinition.MOD, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                        default: Debug.Assert(false); break;
                    }

                    break;

                case UnaryExpressionNode n:
                    this.Visit(n.Expression);

                    switch (n.Op.Operator) {
                        case Operator.UnaryPlus: break;
                        case Operator.UnaryMinus: this.Emit(InstructionDefinition.MUL, Emitter.StackParam, Emitter.StackParam, Parameter.CreateLiteral(ulong.MaxValue)); break;
                        default: Debug.Assert(false); break;
                    }

                    break;

                case NumberNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral((ulong)n.Number));

                    break;

                case IdentifierNode n:
                    this.Visit(n);

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(IdentifierNode e) {
            switch (e) {
                case RegisterNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(n.Register));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(ArgumentListNode argumentList) {
            foreach (var a in argumentList.Arguments)
                this.Visit(a);
        }
    }
}
