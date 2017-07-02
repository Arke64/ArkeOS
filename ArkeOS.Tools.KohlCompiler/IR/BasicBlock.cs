using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Diagnostics;

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
            var globalVars = new List<GlobalVariable>();

            foreach (var i in this.ast.VariableDeclarations.Items)
                globalVars.Add(new GlobalVariable(i.Identifier));

            foreach (var i in this.ast.FunctionDeclarations.Items)
                functions.Add(visitor.Visit(i));

            return new Compiliation(functions, globalVars);
        }
    }

    public class FunctionDeclarationVisitor {
        private readonly NameGenerator nameGenerator = new NameGenerator();
        private List<BasicBlockInstruction> currentInstructions;
        private List<LocalVariable> localVariables;
        private BasicBlock entry;
        private BasicBlock parent;
        private BasicBlock current;

        public Function Visit(FunctionDeclarationNode node) {
            this.currentInstructions = new List<BasicBlockInstruction>();
            this.localVariables = new List<LocalVariable>();
            this.entry = null;
            this.parent = null;
            this.current = null;

            this.Visit(node.StatementBlock);

            this.Push(new ReturnTerminator());

            return new Function(node.Identifier, this.entry, this.localVariables);
        }

        private void Visit(StatementBlockNode node) {
            foreach (var v in node.VariableDeclarations.Items)
                this.localVariables.Add(new LocalVariable(v.Identifier));

            foreach (var b in node.Statements.Items)
                this.Visit(b);
        }

        private void Visit(StatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "statement node");
                case EmptyStatementNode n: break;
                case DeclarationNode n: this.Visit(n); break;

                case AssignmentStatementNode n:
                    var lhs = this.Visit(n.Target);
                    var rhs = this.Visit(n.Expression);

                    this.Push(new BasicBlockAssignmentInstruction(lhs, new ReadLValue(rhs)));

                    break;

                case WhileStatementNode n: Debug.Assert(false); break;
                case IntrinsicStatementNode n: Debug.Assert(false); break;
                case IfStatementNode n: Debug.Assert(false); break;
                case ReturnStatementNode n: Debug.Assert(false); break;
            }
        }

        private void Visit(DeclarationNode node) {
            switch (node) {
                case VariableDeclarationWithInitializerNode n: Debug.Assert(false); break;
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
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

                    return this.CreateVariableAndAssign(new UnaryOperation((UnaryOperationType)n.Op.Operator, e));
            }
        }

        private LValue Visit(IdentifierExpressionNode node) {
            switch (node) {
                case VariableIdentifierNode n: return new LocalVariable(n.Identifier);
                case RegisterIdentifierNode n: return new RegisterVariable(n.Register);
                case FunctionCallIdentifierNode n: Debug.Assert(false); return null;
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
            }
        }

        private LValue Visit(LiteralExpressionNode node) {
            switch (node) {
                case IntegerLiteralNode n: return this.CreateVariableAndAssign(new UnsignedIntegerConstant(n.Literal));
                case BoolLiteralNode n: return this.CreateVariableAndAssign(new UnsignedIntegerConstant(n.Literal ? ulong.MaxValue : 0));
                default: throw new UnexpectedException(default(PositionInfo), "literal node");
            }
        }

        private LValue CreateVariable() {
            var ident = new LocalVariable(this.nameGenerator.Next());

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
                if (this.parent.Terminator is CallTerminator c) {
                    //c.SetReturn(this.current);
                }
                else if (this.parent.Terminator is IfTerminator i) {
                    //if (i.WhenTrue == null) {
                    //    i.SetWhenTrue(this.current);
                    //    goto skipSetParent;
                    //}
                    //else if (i.WhenFalse == null) {
                    //    i.SetWhenFalse(this.current);
                    //}
                }
            }

            this.parent = this.current;

            skipSetParent:
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
        public string Identifier { get; }
        public LValue Argument1 { get; }
        public LValue Argument2 { get; }
        public LValue Argument3 { get; }

        public BasicBlockIntrinsicInstruction(string identifier, LValue argument1, LValue argument2, LValue argument3) => (this.Identifier, this.Argument1, this.Argument2, this.Argument3) = (identifier, argument1, argument2, argument3);

        public override string ToString() => $"{this.Identifier} {this.Argument1} {this.Argument2} {this.Argument3}";
    }

    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariable> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariable> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public abstract class LValue {

    }

    public abstract class Variable : LValue {
        public string Identifier { get; }

        public Variable(string identifier) => this.Identifier = identifier;

        public override string ToString() => this.Identifier;
    }

    public sealed class FunctionArgument : Variable {
        public FunctionArgument(string identifier) : base(identifier) { }
    }

    public sealed class GlobalVariable : Variable {
        public GlobalVariable(string identifier) : base(identifier) { }
    }

    public sealed class LocalVariable : Variable {
        public LocalVariable(string identifier) : base(identifier) { }
    }

    public sealed class RegisterVariable : LValue {
        public Register Register { get; }

        public RegisterVariable(Register register) => this.Register = register;

        public override string ToString() => this.Register.ToString();
    }

    public sealed class Pointer : LValue {
        public LValue Reference { get; }

        public Pointer(LValue reference) => this.Reference = reference;

        public override string ToString() => this.Reference.ToString();
    }

    public sealed class Function : LValue {
        public string Identifier { get; }
        public BasicBlock Entry { get; }
        public IReadOnlyCollection<LocalVariable> LocalVariables { get; }

        public Function(string identifier, BasicBlock bb, IReadOnlyCollection<LocalVariable> variables) => (this.Entry, this.LocalVariables, this.Identifier) = (bb, variables, identifier);

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

    }

    public sealed class GotoTerminator : Terminator {
        public BasicBlock Target { get; }

        public GotoTerminator(BasicBlock target) => this.Target = target;
    }

    public sealed class IfTerminator : Terminator {
        public LValue Condition { get; }
        public BasicBlock WhenTrue { get; }
        public BasicBlock WhenFalse { get; }

        public IfTerminator(LValue condition, BasicBlock whenTrue, BasicBlock whenFalse) => (this.Condition, this.WhenFalse, this.WhenFalse) = (condition, whenTrue, whenFalse);
    }

    public sealed class CallTerminator : Terminator {
        public Function ToCall { get; }
        public LValue ReturnTarget { get; }
        public IReadOnlyCollection<FunctionArgument> Arguments { get; }
        public BasicBlock AfterReturn { get; }

        public CallTerminator(Function toCall, LValue returnTarget, IReadOnlyCollection<FunctionArgument> arguments, BasicBlock afterReturn) => (this.ToCall, this.ReturnTarget, this.Arguments, this.AfterReturn) = (toCall, returnTarget, arguments, afterReturn);
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
