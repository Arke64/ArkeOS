using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public class NameGenerator {
        private ulong nextId = 0;

        public string Next() => "_" + this.nextId++.ToString();
    }

    public class IrGenerator {
        private IrGenerator() { }

        public static Compiliation LowerIr(ProgramDeclarationNode root) {
            var nameGenerator = new NameGenerator();
            var functions = new List<Function>();
            var globalVars = new List<GlobalVariable>();

            foreach (var i in root.VariableDeclarations.Items)
                globalVars.Add(new GlobalVariable(i.Identifier));

            foreach (var i in root.FunctionDeclarations.Items)
                functions.Add(FunctionDeclarationVisitor.Visit(nameGenerator, i));

            return new Compiliation(functions, globalVars);
        }
    }

    public class FunctionDeclarationVisitor {
        private NameGenerator nameGenerator;
        private StatementBlockNode result;

        private FunctionDeclarationVisitor(NameGenerator nameGenerator) => (this.nameGenerator, this.result) = (nameGenerator, new StatementBlockNode());

        private List<BasicBlockInstruction> currentInstructions = new List<BasicBlockInstruction>();
        private List<LocalVariable> localVariables = new List<LocalVariable>();
        private BasicBlock entry;
        private BasicBlock parent;
        private BasicBlock current;

        public static Function Visit(NameGenerator nameGenerator, FunctionDeclarationNode node) {
            var v = new FunctionDeclarationVisitor(nameGenerator);

            v.Visit(node.StatementBlock);

            v.Push(new ReturnTerminator());

            return new Function(v.entry, v.localVariables);
        }

        private LValue CreateAssignment(RValue rhs) {
            var id = this.nameGenerator.Next();
            var ident = new LocalVariable(id);

            this.localVariables.Add(ident);

            this.Push(new BasicBlockAssignmentInstruction(ident, rhs));

            return ident;
        }

        private LValue Visit(ExpressionStatementNode node) {
            switch (node) {
                case VariableIdentifierNode n: return this.ExtractLValue(n);
                case RegisterIdentifierNode n: return this.ExtractLValue(n);
                case IntegerLiteralNode n: return this.CreateAssignment(this.ExtractRValue(n));
                case BoolLiteralNode n: return this.CreateAssignment(this.ExtractRValue(n));

                case BinaryExpressionNode n:
                    var l = this.Visit(n.Left);
                    var r = this.Visit(n.Right);

                    return this.CreateAssignment(new BinaryOperation(l, (BinaryOperationType)n.Op.Operator, r));
            }

            Debug.Assert(false);

            return null;
        }

        private void Visit(StatementBlockNode node) {
            foreach (var v in node.VariableDeclarations.Items)
                this.localVariables.Add(new LocalVariable(v.Identifier));

            foreach (var b in node.Statements.Items)
                this.Visit(b);
        }

        private void Visit(StatementNode node) {
            switch (node) {
                case AssignmentStatementNode n:
                    var lhs = this.Visit(n.Target);
                    var rhs = this.Visit(n.Expression);

                    this.Push(new BasicBlockAssignmentInstruction(lhs, new ReadLValue(rhs)));

                    break;

                default: Debug.Assert(false); break;
            }
        }

        private void Push(Terminator terminator) {
            this.current = new BasicBlock(this.currentInstructions, terminator);

            if (this.parent != null) {
                if (this.parent.Terminator is CallTerminator c) {
                    c.SetReturn(this.current);
                }
                else if (this.parent.Terminator is IfTerminator i) {
                    if (i.WhenTrue == null) {
                        i.SetWhenTrue(this.current);
                        goto skipSetParent;
                    }
                    else if (i.WhenFalse == null) {
                        i.SetWhenFalse(this.current);
                    }
                }
            }

            this.parent = this.current;

            skipSetParent:
            this.currentInstructions = new List<BasicBlockInstruction>();

            if (this.entry == null)
                this.entry = this.parent;
        }

        private void Push(BasicBlockInstruction bbi) => this.currentInstructions.Add(bbi);

        private LValue ExtractLValue(ExpressionStatementNode e) {
            switch (e) {
                case RegisterIdentifierNode n: return new RegisterVariable(n.Register);
                case VariableIdentifierNode n: return new LocalVariable(n.Identifier);
                default: Debug.Assert(false); return null;
            }
        }

        private RValue ExtractRValue(ExpressionStatementNode e) {
            switch (e) {
                case RegisterIdentifierNode n: return new ReadLValue(new RegisterVariable(n.Register));
                case VariableIdentifierNode n: return new ReadLValue(new LocalVariable(n.Identifier));
                case IntegerLiteralNode n: return new UnsignedIntegerConstant(n.Literal);
                case BoolLiteralNode n: return new UnsignedIntegerConstant(n.Literal ? ulong.MaxValue : 0);
                default: Debug.Assert(false); return null;
            }
        }
    }

    public sealed class BasicBlock {
        public IReadOnlyCollection<BasicBlockInstruction> Instructions { get; }
        public Terminator Terminator { get; }

        public BasicBlock(IReadOnlyCollection<BasicBlockInstruction> instructions, Terminator terminator) => (this.Instructions, this.Terminator) = (instructions, terminator);
    }

    public abstract class BasicBlockInstruction { }

    public sealed class BasicBlockAssignmentInstruction : BasicBlockInstruction {
        public LValue Target { get; }
        public RValue Value { get; }

        public BasicBlockAssignmentInstruction(LValue target, RValue value) => (this.Target, this.Value) = (target, value);

        public override string ToString() => $"{this.Target} = {this.Value}";
    }

    public sealed class BasicBlockIntrinsicInstruction : BasicBlockInstruction {

    }

    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariable> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariable> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public abstract class LValue { }
    public sealed class FunctionArgument : LValue { }

    public sealed class GlobalVariable : LValue {
        public string Identifier { get; }

        public GlobalVariable(string identifier) => this.Identifier = identifier;

        public override string ToString() => this.Identifier;
    }

    public sealed class LocalVariable : LValue {
        public string Identifier { get; }

        public LocalVariable(string identifier) => this.Identifier = identifier;

        public override string ToString() => this.Identifier;
    }

    public sealed class RegisterVariable : LValue {
        public Register Register { get; }

        public RegisterVariable(Register register) => this.Register = register;

        public override string ToString() => this.Register.ToString();
    }

    public sealed class Pointer : LValue {
        public LValue Reference { get; }
    }

    public sealed class Function : LValue {
        public IReadOnlyCollection<LocalVariable> LocalVariables { get; }
        public BasicBlock Entry { get; }

        public Function(BasicBlock bb, IReadOnlyCollection<LocalVariable> variables) => (this.Entry, this.LocalVariables) = (bb, variables);
    }

    public abstract class RValue { }
    public abstract class Constant : RValue { }

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

        public override string ToString() => $"'{this.Op}' {this.Value}";
    }

    public abstract class Terminator { }

    public sealed class ReturnTerminator : Terminator { }

    public sealed class GotoTerminator : Terminator {
        public BasicBlock Target { get; }
    }

    public sealed class IfTerminator : Terminator {
        public LValue Condition { get; }
        public BasicBlock WhenTrue { get; private set; }
        public BasicBlock WhenFalse { get; private set; }

        public void SetWhenTrue(BasicBlock whenTrue) => this.WhenTrue = whenTrue;
        public void SetWhenFalse(BasicBlock whenFalse) => this.WhenFalse = whenFalse;
    }

    public sealed class CallTerminator : Terminator {
        public LValue ReturnTarget { get; }
        public LValue ToCall { get; }
        public IReadOnlyCollection<LValue> Arguments { get; }
        public BasicBlock OnReturn { get; private set; }

        public void SetReturn(BasicBlock onReturn) => this.OnReturn = onReturn;
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
