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

            var start = Parameter.CreateLiteral(false, true, 0);
            var len = Parameter.CreateLiteral(false, false, 0);

            this.Emit(InstructionDefinition.CPY, Parameter.CreateRegister(false, false, Register.RZERO), start, len);
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(false, false, Register.RIP), Parameter.CreateRegister(false, false, Register.RZERO));

            start.Literal = (ulong)this.instructions.Sum(i => i.Length);

            this.Visit(this.tree);

            this.Emit(InstructionDefinition.HLT);

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

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def.Code, parameters, null, false));
        private void Emit(InstructionDefinition def, Parameter conditional, bool conditionalZero, params Parameter[] parameters) => this.instructions.Add(new Instruction(def.Code, parameters, conditional, conditionalZero));

        private void Visit(ProgramNode n) {
            foreach (var s in n.Statements)
                this.Visit(s);
        }

        private void Visit(StatementNode s) {
            switch (s) {
                case AssignmentNode n:
                    this.Visit(n.Expression);

                    this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(false, false, n.Target.Identifier.ToEnum<Register>()), Emitter.StackParam);

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
                        case Operator.UnaryMinus: this.Emit(InstructionDefinition.MUL, Emitter.StackParam, Emitter.StackParam, Parameter.CreateLiteral(false, false, ulong.MaxValue)); break;
                        default: Debug.Assert(false); break;
                    }

                    break;

                case NumberNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral(false, false, (ulong)n.Number));

                    break;

                case IdentifierNode n:
                    this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(false, false, n.Identifier.ToEnum<Register>()));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
