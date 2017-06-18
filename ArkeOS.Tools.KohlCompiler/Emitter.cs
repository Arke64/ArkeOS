using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Nodes;
using ArkeOS.Utilities.Extensions;
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

            var start = Parameter.CreateLiteral(0, ParameterFlags.RIPRelative);
            var len = Parameter.CreateLiteral(0);

            this.Emit(InstructionDefinition.CPY, Parameter.CreateRegister(Register.RZERO), start, len);

            start.Literal = (ulong)this.instructions.Sum(i => i.Length);

            this.Visit(this.tree);

            len.Literal = (ulong)this.instructions.Sum(i => i.Length) - start.Literal;

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

        private void Visit(ProgramNode n) => this.Visit(n.StatementBlock);

        private void Visit(StatementBlockNode n) {
            foreach (var s in n.Statements)
                this.Visit(s);
        }

        private void Visit(StatementNode s) {
            switch (s) {
                case IfStatementNode n:
                    this.Visit(n.Expression);

                    this.Visit(n.Statement);

                    var stmt = this.instructions.Last();

                    this.instructions.Remove(stmt);

                    this.instructions.Add(new Instruction(stmt.Definition, new[] { stmt.Parameter1, stmt.Parameter2, stmt.Parameter3 }, Parameter.CreateStack(), InstructionConditionalType.WhenNotZero));

                    break;

                case AssignmentStatementNode n:
                    if (n.Expression is IdentifierNode inode) {
                        this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(n.Target.Identifier.ToEnum<Register>()), Parameter.CreateRegister(inode.Identifier.ToEnum<Register>()));
                    }
                    else if (n.Expression is NumberNode nnode) {
                        this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(n.Target.Identifier.ToEnum<Register>()), Parameter.CreateLiteral((ulong)nnode.Number));
                    }
                    else {
                        this.Visit(n.Expression);

                        this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(n.Target.Identifier.ToEnum<Register>()), Emitter.StackParam);
                    }


                    break;

                case BrkStatementNode n: this.Emit(InstructionDefinition.BRK); break;
                case EintStatementNode n: this.Emit(InstructionDefinition.EINT); break;
                case HltStatementNode n: this.Emit(InstructionDefinition.HLT); break;
                case IntdStatementNode n: this.Emit(InstructionDefinition.INTD); break;
                case InteStatementNode n: this.Emit(InstructionDefinition.INTE); break;
                case NopStatementNode n: this.Emit(InstructionDefinition.NOP); break;
                case IntStatementNode n: this.Visit(n.A); this.Visit(n.B); this.Visit(n.C); this.Emit(InstructionDefinition.INT, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                case CasStatementNode n: this.Visit(n.C); this.Emit(InstructionDefinition.CAS, Parameter.CreateRegister(n.A.Identifier.ToEnum<Register>()), Parameter.CreateRegister(n.B.Identifier.ToEnum<Register>()), Emitter.StackParam); break;
                case CpyStatementNode n: this.Visit(n.A); this.Visit(n.B); this.Visit(n.C); this.Emit(InstructionDefinition.CPY, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                case DbgStatementNode n: this.Visit(n.A); this.Visit(n.B); this.Visit(n.C); this.Emit(InstructionDefinition.DBG, Emitter.StackParam, Emitter.StackParam, Emitter.StackParam); break;
                case XchgStatementNode n: this.Emit(InstructionDefinition.XCHG, Parameter.CreateRegister(n.A.Identifier.ToEnum<Register>()), Parameter.CreateRegister(n.B.Identifier.ToEnum<Register>())); break;

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

                case ValueNode n:
                    this.Visit(n);

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(ValueNode v) {
            switch (v) {
                case NumberNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral((ulong)n.Number));

                    break;

                case IdentifierNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(n.Identifier.ToEnum<Register>()));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
