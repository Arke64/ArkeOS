﻿using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public class NameGenerator {
        private ulong nextId = 0;

        public string Next() => "_" + this.nextId++.ToString();
    }

    public class IrGenerator {
        private readonly ProgramDeclarationNode ast;

        public IrGenerator(ProgramDeclarationNode ast) => this.ast = ast;

        public Compiliation Generate() {
            var visitor = new FunctionDeclarationVisitor();
            var functions = new List<Function>();
            var globalVars = new List<GlobalVariableLValue>();

            foreach (var i in this.ast.VariableDeclarations.Items)
                globalVars.Add(new GlobalVariableLValue(i.Identifier));

            foreach (var i in this.ast.FunctionDeclarations.Items)
                functions.Add(visitor.Visit(i));

            return new Compiliation(functions, globalVars);
        }
    }

    public class FunctionDeclarationVisitor {
        private readonly NameGenerator nameGenerator = new NameGenerator();
        private List<BasicBlockInstruction> currentInstructions;
        private List<LocalVariableLValue> localVariables;
        private BasicBlock entry;
        private BasicBlock parent;
        private BasicBlock current;

        public Function Visit(FunctionDeclarationNode node) {
            this.currentInstructions = new List<BasicBlockInstruction>();
            this.localVariables = new List<LocalVariableLValue>();
            this.entry = null;
            this.parent = null;
            this.current = null;

            this.Visit(node.StatementBlock);

            this.Push(new ReturnTerminator(this.CreateVariable()));

            return new Function(node.Identifier, this.entry, node.ArgumentListDeclaration.Items.Select(i => i.Identifier).ToList(), this.localVariables);
        }

        private void Visit(StatementBlockNode node) {
            foreach (var v in node.VariableDeclarations.Items)
                this.localVariables.Add(new LocalVariableLValue(v.Identifier));

            foreach (var b in node.Statements.Items)
                this.Visit(b);
        }

        private void Visit(StatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "statement node");
                case EmptyStatementNode n: break;
                case DeclarationNode n: this.Visit(n); break;
                case ReturnStatementNode n: this.Push(new ReturnTerminator(this.Visit(n.Expression))); break;
                case ExpressionStatementNode n: this.Visit(n); break;
                case IntrinsicStatementNode n: this.Visit(n); break;

                case AssignmentStatementNode n:
                    var lhs = this.Visit(n.Target);
                    var rhs = this.Visit(n.Expression);

                    this.Push(new BasicBlockAssignmentInstruction(lhs, new ReadLValue(rhs)));

                    break;

                case IfStatementNode n:
                    var term1 = new GotoTerminator();
                    var entry = this.entry;
                    var parent = this.parent;
                    var insts = this.currentInstructions.Select(i => i).ToList();

                    this.currentInstructions = new List<BasicBlockInstruction>();

                    this.entry = null;
                    this.parent = null;
                    this.Visit(n.StatementBlock);
                    this.Push(term1);
                    var ifEntry = this.entry;

                    this.entry = null;
                    this.parent = null;
                    if (n is IfElseStatementNode ie)
                        this.Visit(ie.ElseStatementBlock);
                    this.Push(term1);
                    var elseEntry = this.entry;

                    this.entry = entry;
                    this.parent = parent;
                    this.currentInstructions = insts;

                    this.Push(new IfTerminator(this.Visit(n.Expression), ifEntry, elseEntry));

                    break;

                case WhileStatementNode n: Debug.Assert(false); break;
            }
        }

        private void Visit(DeclarationNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case VariableDeclarationWithInitializerNode n:
                    var i = this.Visit(n.Initializer);

                    this.Push(new BasicBlockAssignmentInstruction(new LocalVariableLValue(n.Identifier), new ReadLValue(i)));

                    break;
            }
        }

        private LValue Visit(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "expression node");
                case IdentifierExpressionNode n: return this.Visit(n);
                case LiteralExpressionNode n: return this.Visit(n);

                case BinaryExpressionNode n:
                    var l = this.Visit(n.Left);
                    var r = this.Visit(n.Right);

                    return this.CreateVariableAndAssign(new BinaryOperation(l, (BinaryOperationType)n.Op.Operator, r));

                case UnaryExpressionNode n:
                    var e = this.Visit(n.Expression);

                    return n.Op.Operator != Operator.Dereference ? this.CreateVariableAndAssign(new UnaryOperation((UnaryOperationType)n.Op.Operator, e)) : new PointerLValue(e);
            }
        }

        private LValue Visit(IdentifierExpressionNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case VariableIdentifierNode n: return new LocalVariableLValue(n.Identifier);
                case RegisterIdentifierNode n: return new RegisterLValue(n.Register);

                case FunctionCallIdentifierNode n:
                    var ident = this.CreateVariable();
                    var args = new List<FunctionArgumentLValue>();

                    foreach (var a in n.ArgumentList.Items)
                        args.Add(new FunctionArgumentLValue(this.Visit(a)));

                    this.Push(new FunctionCallTerminator(n.Identifier, ident, args));

                    return ident;
            }
        }

        private LValue Visit(LiteralExpressionNode node) {
            switch (node) {
                case IntegerLiteralNode n: return this.CreateVariableAndAssign(new UnsignedIntegerConstant(n.Literal));
                case BoolLiteralNode n: return this.CreateVariableAndAssign(new UnsignedIntegerConstant(n.Literal ? ulong.MaxValue : 0));
                default: throw new UnexpectedException(default(PositionInfo), "literal node");
            }
        }

        private void Visit(IntrinsicStatementNode s) {
            switch (s) {
                case BrkStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.BRK, null, null, null)); break;
                case EintStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.EINT, null, null, null)); break;
                case HltStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.HLT, null, null, null)); break;
                case IntdStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.INTD, null, null, null)); break;
                case InteStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.INTE, null, null, null)); break;
                case NopStatementNode n: this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.NOP, null, null, null)); break;

                case CpyStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            var a = this.Visit(arg0);
                            var b = this.Visit(arg1);
                            var c = this.Visit(arg2);

                            this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.CPY, a, b, c));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case IntStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            var a = this.Visit(arg0);
                            var b = this.Visit(arg1);
                            var c = this.Visit(arg2);

                            this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.INT, a, b, c));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case DbgStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            var a = this.Visit(arg0);
                            var b = this.Visit(arg1);
                            var c = this.Visit(arg2);

                            this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.DBG, a, b, c));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case CasStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1, out var arg2)) {
                            var a = this.Visit(arg0);
                            var b = this.Visit(arg1);
                            var c = this.Visit(arg2);

                            this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.CAS, a, b, c));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                case XchgStatementNode n: {
                        if (n.ArgumentList.Extract(out var arg0, out var arg1)) {
                            var a = this.Visit(arg0);
                            var b = this.Visit(arg1);

                            this.Push(new BasicBlockIntrinsicInstruction(InstructionDefinition.XCHG, a, b, null));
                        }
                        else {
                            throw new TooFewArgumentsException(default(PositionInfo));
                        }
                    }

                    break;

                default: Debug.Assert(false); break;
            }
        }

        private LValue CreateVariable() {
            var ident = new LocalVariableLValue(this.nameGenerator.Next());

            this.localVariables.Add(ident);

            return ident;
        }

        private LValue CreateVariableAndAssign(RValue rhs) {
            var ident = this.CreateVariable();

            this.Push(new BasicBlockAssignmentInstruction(ident, rhs));

            return ident;
        }

        private void Push(Terminator terminator) {
            this.current = new BasicBlock(this.currentInstructions, terminator);

            if (this.parent != null) {
                if (this.parent.Terminator is FunctionCallTerminator c) {
                    c.SetAfterReturn(this.current);
                }
                else if (this.parent.Terminator is GotoTerminator g) {
                    g.SetTarget(this.current);
                }
                else if (this.parent.Terminator is IfTerminator i) {
                    if (i.WhenTrue.Terminator is GotoTerminator g1) {
                        g1.SetTarget(this.current);
                    }
                    if (i.WhenFalse.Terminator is GotoTerminator g2) {
                        g2.SetTarget(this.current);
                    }
                }
            }

            this.parent = this.current;

            this.currentInstructions = new List<BasicBlockInstruction>();

            if (this.entry == null)
                this.entry = this.parent;
        }

        private void Push(BasicBlockInstruction bbi) => this.currentInstructions.Add(bbi);
    }

    public sealed class BasicBlock {
        public IReadOnlyCollection<BasicBlockInstruction> Instructions { get; }
        public Terminator Terminator { get; }

        public BasicBlock(IReadOnlyCollection<BasicBlockInstruction> instructions, Terminator terminator) => (this.Instructions, this.Terminator) = (instructions, terminator);
    }

    public abstract class BasicBlockInstruction {

    }

    public sealed class BasicBlockAssignmentInstruction : BasicBlockInstruction {
        public LValue Target { get; }
        public RValue Value { get; }

        public BasicBlockAssignmentInstruction(LValue target, RValue value) => (this.Target, this.Value) = (target, value);

        public override string ToString() => $"{this.Target} = {this.Value}";
    }

    public sealed class BasicBlockIntrinsicInstruction : BasicBlockInstruction {
        public InstructionDefinition Intrinsic { get; }
        public LValue Argument1 { get; }
        public LValue Argument2 { get; }
        public LValue Argument3 { get; }

        public BasicBlockIntrinsicInstruction(InstructionDefinition inst, LValue argument1, LValue argument2, LValue argument3) => (this.Intrinsic, this.Argument1, this.Argument2, this.Argument3) = (inst, argument1, argument2, argument3);

        public override string ToString() => $"{this.Intrinsic.Mnemonic} {this.Argument1} {this.Argument2} {this.Argument3}";
    }

    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariableLValue> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariableLValue> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public abstract class LValue {

    }

    public abstract class Variable : LValue {
        public string Identifier { get; }

        public Variable(string identifier) => this.Identifier = identifier;

        public override string ToString() => this.Identifier;
    }

    public sealed class GlobalVariableLValue : Variable {
        public GlobalVariableLValue(string identifier) : base(identifier) { }
    }

    public sealed class LocalVariableLValue : Variable {
        public LocalVariableLValue(string identifier) : base(identifier) { }
    }

    public sealed class RegisterLValue : LValue {
        public Register Register { get; }

        public RegisterLValue(Register register) => this.Register = register;

        public override string ToString() => this.Register.ToString();
    }

    public sealed class FunctionArgumentLValue : LValue {
        public LValue Argument { get; }

        public FunctionArgumentLValue(LValue argument) => this.Argument = argument;

        public override string ToString() => this.Argument.ToString();
    }

    public sealed class PointerLValue : LValue {
        public LValue Reference { get; }

        public PointerLValue(LValue reference) => this.Reference = reference;

        public override string ToString() => this.Reference.ToString();
    }

    public sealed class Function : LValue {
        public string Identifier { get; }
        public BasicBlock Entry { get; }
        public IReadOnlyCollection<string> Arguments { get; }
        public IReadOnlyCollection<LocalVariableLValue> LocalVariables { get; }

        public Function(string identifier, BasicBlock bb, IReadOnlyCollection<string> arguments, IReadOnlyCollection<LocalVariableLValue> variables) => (this.Entry, this.Arguments, this.LocalVariables, this.Identifier) = (bb, arguments, variables, identifier);

        public override string ToString() => this.Identifier;
    }

    public abstract class RValue {

    }

    public abstract class Constant : RValue {

    }

    public sealed class UnsignedIntegerConstant : Constant {
        public ulong Value { get; }

        public UnsignedIntegerConstant(ulong value) => this.Value = value;

        public override string ToString() => this.Value.ToString();
    }

    public sealed class ReadLValue : RValue {
        public LValue Value { get; }

        public ReadLValue(LValue value) => this.Value = value;

        public override string ToString() => this.Value.ToString();
    }

    public sealed class BinaryOperation : RValue {
        public LValue Left { get; }
        public BinaryOperationType Op { get; }
        public LValue Right { get; }

        public BinaryOperation(LValue left, BinaryOperationType op, LValue right) => (this.Left, this.Op, this.Right) = (left, op, right);

        public override string ToString() => $"{this.Left} '{this.Op}' {this.Right}";
    }

    public sealed class UnaryOperation : RValue {
        public UnaryOperationType Op { get; }
        public LValue Value { get; }

        public UnaryOperation(UnaryOperationType op, LValue value) => (this.Op, this.Value) = (op, value);

        public override string ToString() => $"'{this.Op}' {this.Value}";
    }

    public abstract class Terminator {

    }

    public sealed class ReturnTerminator : Terminator {
        public LValue Value { get; }

        public ReturnTerminator(LValue value) => this.Value = value;
    }

    public sealed class GotoTerminator : Terminator {
        public BasicBlock Target { get; private set; }

        public void SetTarget(BasicBlock target) => this.Target = target;
    }

    public sealed class IfTerminator : Terminator {
        public LValue Condition { get; }
        public BasicBlock WhenTrue { get; }
        public BasicBlock WhenFalse { get; }

        public IfTerminator(LValue condition, BasicBlock whenTrue, BasicBlock whenFalse) => (this.Condition, this.WhenTrue, this.WhenFalse) = (condition, whenTrue, whenFalse);
    }

    public sealed class FunctionCallTerminator : Terminator {
        public string ToCall { get; }
        public LValue ReturnTarget { get; }
        public IReadOnlyCollection<FunctionArgumentLValue> Arguments { get; }
        public BasicBlock AfterReturn { get; private set; }

        public void SetAfterReturn(BasicBlock afterReturn) => this.AfterReturn = afterReturn;

        public FunctionCallTerminator(string toCall, LValue returnTarget, IReadOnlyCollection<FunctionArgumentLValue> arguments) => (this.ToCall, this.ReturnTarget, this.Arguments) = (toCall, returnTarget, arguments);
    }

    public enum BinaryOperationType {
        Addition = Operator.Addition,
        Subtraction = Operator.Subtraction,
        Multiplication = Operator.Multiplication,
        Division = Operator.Division,
        Remainder = Operator.Remainder,
        Exponentiation = Operator.Exponentiation,
        ShiftLeft = Operator.ShiftLeft,
        ShiftRight = Operator.ShiftRight,
        RotateLeft = Operator.RotateLeft,
        RotateRight = Operator.RotateRight,
        And = Operator.And,
        Or = Operator.Or,
        Xor = Operator.Xor,
        NotAnd = Operator.NotAnd,
        NotOr = Operator.NotOr,
        NotXor = Operator.NotXor,
        Equals = Operator.Equals,
        NotEquals = Operator.NotEquals,
        LessThan = Operator.LessThan,
        LessThanOrEqual = Operator.LessThanOrEqual,
        GreaterThan = Operator.GreaterThan,
        GreaterThanOrEqual = Operator.GreaterThanOrEqual,
    }

    public enum UnaryOperationType {
        Plus = Operator.Addition,
        Minus = Operator.Subtraction,
        Not = Operator.Not,
        AddressOf = Operator.AddressOf,
        Dereference = Operator.Dereference,
    }
}
