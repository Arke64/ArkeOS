using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class Emitter {
        private static Parameter StackParam { get; } = new Parameter { Type = ParameterType.Stack };

        private readonly ProgramDeclarationNode tree;
        private readonly bool emitAssemblyListing;
        private readonly bool emitBootable;
        private readonly string outputFile;
        private List<Instruction> instructions;
        private Dictionary<string, ulong> functionAddresses;
        private bool throwOnNoFunction;

        public Emitter(ProgramDeclarationNode tree, bool emitAssemblyListing, bool emitBootable, string outputFile) => (this.tree, this.emitAssemblyListing, this.emitBootable, this.outputFile) = (tree, emitAssemblyListing, emitBootable, outputFile);

        private ulong DistanceFrom(int startInst) => (ulong)this.instructions.Skip(startInst).Sum(i => i.Length);

        public void Emit() {
            this.functionAddresses = new Dictionary<string, ulong>();
            this.instructions = new List<Instruction>();
            this.throwOnNoFunction = false;

            this.EmitHeader();
            this.DiscoverAddresses(this.tree);

            this.instructions.Clear();
            this.throwOnNoFunction = true;

            this.EmitHeader();
            this.Visit(this.tree);

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    if (this.emitBootable)
                        writer.Write(0x00000000454B5241UL);

                    if (this.emitAssemblyListing)
                        File.WriteAllLines(Path.ChangeExtension(this.outputFile, "lst"), this.instructions.Select(i => i.ToString()));

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(this.outputFile, stream.ToArray());
                }
            }
        }

        private void EmitHeader() {
            this.Emit(InstructionDefinition.BRK);
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RSP), Parameter.CreateLiteral(0x1_0000));

            this.Emit(InstructionDefinition.CALL, this.GetAddress("main"));
            this.Emit(InstructionDefinition.HLT);
        }

        private void DiscoverAddresses(ProgramDeclarationNode n) {
            foreach (var s in n.Items) {
                if (this.functionAddresses.ContainsKey(s.Identifier))
                    throw new AlreadyDefinedException(default(PositionInfo), s.Identifier);

                this.functionAddresses[s.Identifier] = (ulong)this.instructions.Sum(i => i.Length);

                this.Visit(s);
            }
        }

        private Parameter GetAddress(string func) {
            if (!this.functionAddresses.TryGetValue(func, out var addr) && this.throwOnNoFunction)
                throw new IdentifierNotFoundException(default(PositionInfo), func);

            return Parameter.CreateLiteral(addr - this.DistanceFrom(0), ParameterFlags.RelativeToRIP);
        }

        private void Emit(InstructionDefinition def, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters));
        private void Emit(InstructionDefinition def, Parameter conditional, InstructionConditionalType conditionalType, params Parameter[] parameters) => this.instructions.Add(new Instruction(def, parameters, conditional, conditionalType));

        private Parameter ExtractLValue(ExpressionStatementNode expr) {
            switch (expr) {
                case RegisterIdentifierNode n: return Parameter.CreateRegister(n.Register);
                default: throw new ExpectedException(default(PositionInfo), "value");
            }
        }

        private void Visit(ProgramDeclarationNode n) {
            foreach (var s in n.Items)
                this.Visit(s);
        }

        private void Visit(FunctionDeclarationNode n) {
            this.Visit(n.StatementBlock);

            this.Emit(InstructionDefinition.RET);
        }

        private void Visit(StatementBlockNode n) {
            foreach (var s in n.Items)
                this.Visit(s);
        }

        private void Visit(StatementNode s) {
            switch (s) {
                case IntrinsicStatementNode n: this.Visit(n); break;
                case IfStatementNode n: this.Visit(n); break;
                case WhileStatementNode n: this.Visit(n); break;
                case AssignmentStatementNode n: this.Visit(n); break;
                case ExpressionStatementNode n: this.Visit(n); break;
                case EmptyStatementNode n: break;
                default: Debug.Assert(false); break;
            }
        }

        private void Visit(IntrinsicStatementNode s) {
            switch (s) {
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

                default: Debug.Assert(false); break;
            }
        }

        private void Visit(IfStatementNode i) {
            this.Visit(i.Expression);

            var ifStart = this.instructions.Count;
            var ifLen = Parameter.CreateLiteral(0, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, Emitter.StackParam, InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), ifLen);

            this.Visit(i.StatementBlock);

            if (i is IfElseStatementNode ie) {
                var elseStart = this.instructions.Count;
                var elseLen = Parameter.CreateLiteral(0, ParameterFlags.RelativeToRIP);
                this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), elseLen);

                ifLen.Literal = this.DistanceFrom(ifStart);

                this.Visit(ie.ElseStatementBlock);

                elseLen.Literal = this.DistanceFrom(elseStart);
            }
            else {
                ifLen.Literal = this.DistanceFrom(ifStart);
            }
        }

        private void Visit(WhileStatementNode w) {
            var nodeStart = this.instructions.Count;

            this.Visit(w.Expression);

            var blockStart = this.instructions.Count;
            var blockLen = Parameter.CreateLiteral(0, ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, Emitter.StackParam, InstructionConditionalType.WhenZero, Parameter.CreateRegister(Register.RIP), blockLen);

            this.Visit(w.StatementBlock);

            var nodeLen = Parameter.CreateLiteral((ulong)-(long)this.DistanceFrom(nodeStart), ParameterFlags.RelativeToRIP);
            this.Emit(InstructionDefinition.SET, Parameter.CreateRegister(Register.RIP), nodeLen);

            blockLen.Literal = this.DistanceFrom(blockStart);
        }

        private void Visit(AssignmentStatementNode a) {
            if (a is CompoundAssignmentStatementNode c)
                a = new AssignmentStatementNode(c.Target, new BinaryExpressionNode(c.Target, c.Op, c.Expression));

            var target = this.ExtractLValue(a.Target);

            if (a.Expression is RegisterIdentifierNode rnode) {
                this.Emit(InstructionDefinition.SET, target, Parameter.CreateRegister(rnode.Register));
            }
            else if (a.Expression is IntegerLiteralNode nnode) {
                this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral(nnode.Literal));
            }
            else if (a.Expression is BoolLiteralNode bnode) {
                this.Emit(InstructionDefinition.SET, target, Parameter.CreateLiteral(bnode.Literal ? ulong.MaxValue : 0));
            }
            else {
                this.Visit(a.Expression);

                this.Emit(InstructionDefinition.SET, target, Emitter.StackParam);
            }
        }

        private void Visit(ExpressionStatementNode e) {
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

                case IdentifierExpressionNode n: this.Visit(n); break;
                case LiteralExpressionNode n: this.Visit(n); break;
                default: Debug.Assert(false); break;
            }
        }

        private void Visit(IdentifierExpressionNode i) {
            switch (i) {
                case RegisterIdentifierNode n: this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateRegister(n.Register)); break;
                case FunctionCallIdentifierNode n: this.Emit(InstructionDefinition.CALL, this.GetAddress(n.Identifier)); break;
                default: Debug.Assert(false); break;
            }
        }

        private void Visit(LiteralExpressionNode l) {
            switch (l) {
                case IntegerLiteralNode n: this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral(n.Literal)); break;
                case BoolLiteralNode n: this.Emit(InstructionDefinition.SET, Emitter.StackParam, Parameter.CreateLiteral(n.Literal ? ulong.MaxValue : 0)); break;
                default: Debug.Assert(false); break;
            }
        }
    }
}
