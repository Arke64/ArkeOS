using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Nodes;
using ArkeOS.Utilities.Extensions;
using System;
using System.Collections.Generic;
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

            var start = new Parameter { Type = ParameterType.Literal, Literal = 0, IsRIPRelative = true };
            var len = new Parameter { Type = ParameterType.Literal, Literal = 0 };

            this.instructions.Add(new Instruction(InstructionDefinition.Find("CPY").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = Register.RZERO }, start, len }, null, false));
            this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = Register.RIP }, new Parameter { Type = ParameterType.Register, Register = Register.RZERO } }, null, false));

            start.Literal = (ulong)this.instructions.Sum(i => i.Length);

            this.Visit(this.tree);

            this.instructions.Add(new Instruction(InstructionDefinition.Find("HLT").Code, new List<Parameter> { }, null, false));

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

        private void Visit(ProgramNode n) {
            foreach (var s in n.Statements)
                this.Visit(s);
        }

        private void Visit(StatementNode s) {
            switch (s) {
                case AssignmentNode n:
                    this.Visit(n.Expression);

                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = n.Target.Identifier.ToEnum<Register>() }, Emitter.StackParam }, null, false));

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

                    var inst = "";

                    switch (n.Op.Operator) {
                        case Operator.Addition: inst = "ADD"; break;
                        case Operator.Subtraction: inst = "SUB"; break;
                        case Operator.Multiplication: inst = "MUL"; break;
                        case Operator.Division: inst = "DIV"; break;
                        case Operator.Exponentiation: inst = "POW"; break;
                        case Operator.Remainder: inst = "MOD"; break;
                    }

                    this.instructions.Add(new Instruction(InstructionDefinition.Find(inst).Code, new List<Parameter> { Emitter.StackParam, Emitter.StackParam, Emitter.StackParam }, null, false));

                    break;

                case UnaryExpressionNode n:
                    this.Visit(n.Expression);

                    switch (n.Op.Operator) {
                        case Operator.UnaryPlus: break;
                        case Operator.UnaryMinus: this.instructions.Add(new Instruction(InstructionDefinition.Find("MUL").Code, new List<Parameter> { Emitter.StackParam, Emitter.StackParam, new Parameter { Type = ParameterType.Literal, Literal = unchecked((ulong)(-1)) } }, null, false)); break;
                        default: throw new InvalidOperationException("Unexpected operator.");
                    }

                    break;

                case NumberNode n:
                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { Emitter.StackParam, new Parameter { Type = ParameterType.Literal, Literal = (ulong)n.Number } }, null, false));

                    break;

                case IdentifierNode n:
                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { Emitter.StackParam, new Parameter { Type = ParameterType.Register, Register = n.Identifier.ToEnum<Register>() } }, null, false));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
