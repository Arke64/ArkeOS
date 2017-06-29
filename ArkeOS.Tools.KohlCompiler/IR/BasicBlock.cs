using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public enum SymbolType {
        GlobalVariable,
        Constant,
        LocalVariable,
        FunctionArgument,
        Function
    }

    public class SymbolTable {
        public bool TryGet(string name, SymbolType type, int scope, out object result) {
            throw null;
        }
    }

    public class IrGenerator {
        private readonly ProgramDeclarationNode root;
        private readonly Dictionary<string, Function> functions = new Dictionary<string, Function>();
        private readonly Dictionary<string, GlobalVariable> globalVariables = new Dictionary<string, GlobalVariable>();

        public IrGenerator(ProgramDeclarationNode root) => this.root = root;

        public Compiliation Generate() {
            foreach (var f in this.root.FunctionDeclarations.Items)
                this.functions[f.Identifier] = this.Visit(f);

            return new Compiliation(this.functions.Values, this.globalVariables.Values);
        }

        private Function Visit(FunctionDeclarationNode n) => FunctionVisitor.Visit(n);
    }

    public class FunctionVisitor {
        private readonly FunctionDeclarationNode func;
        private List<BasicBlockInstruction> currentInstructions = new List<BasicBlockInstruction>();
        private BasicBlock entry;
        private BasicBlock parent;
        private BasicBlock current;

        private FunctionVisitor(FunctionDeclarationNode func) => this.func = func;

        public static Function Visit(FunctionDeclarationNode func) => new FunctionVisitor(func).Visit();

        private Function Visit() {
            this.Visit(this.func.StatementBlock);

            this.Push(new ReturnTerminator());

            return new Function(this.entry);
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

        private void Visit(StatementBlockNode sbn) {
            foreach (var s in sbn.Statements.Items) {
                switch (s) {
                    case AssignmentStatementNode n:
                        if (n.Target is RegisterIdentifierNode r && n.Expression is IntegerLiteralNode i) {
                            this.Push(new BasicBlockAssignmentInstruction(new RegisterVariable(r.Register), new UnsignedIntegerConstant(i.Literal)));
                        }

                        break;
                }
            }
        }

        private RValue Visit(ExpressionStatementNode esn) {
            switch (esn) {
                case IntegerLiteralNode n: return new UnsignedIntegerConstant(n.Literal);
                default: throw new NotImplementedException();
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
    }

    public sealed class BasicBlockIntrinsicInstruction : BasicBlockInstruction {

    }

    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariable> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariable> globalVariables) => (this.Functions, this.GlobalVariables) = (functions, globalVariables);
    }

    public abstract class LValue { }
    public sealed class LocalVariable : LValue { }
    public sealed class GlobalVariable : LValue { }
    public sealed class FunctionArgument : LValue { }

    public sealed class RegisterVariable : LValue {
        public Register Register { get; }

        public RegisterVariable(Register register) => this.Register = register;
    }

    public sealed class Pointer : LValue {
        public LValue Reference { get; }
    }

    public sealed class Function : LValue {
        public IReadOnlyCollection<LocalVariable> LocalVariables { get; }
        public BasicBlock Entry { get; }

        public Function(BasicBlock bb) => this.Entry = bb;
    }

    public abstract class RValue { }
    public abstract class Constant : RValue { }

    public sealed class UnsignedIntegerConstant : Constant {
        public ulong Value { get; }

        public UnsignedIntegerConstant(ulong value) => this.Value = value;
    }

    public sealed class ReadLValue : RValue {
        public LValue Value { get; }
    }

    public sealed class BinaryOperation : RValue {
        public LValue Left { get; }
        public BinaryOperationType Op { get; }
        public LValue Right { get; }
    }

    public sealed class UnaryOperation : RValue {
        public UnaryOperationType Op { get; }
        public LValue Value { get; }
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
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Remainder,
        Exponentiation,
        ShiftLeft,
        ShiftRight,
        RotateLeft,
        RotateRight,
        And,
        Or,
        Xor,
        NotAnd,
        NotOr,
        NotXor,
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Not,
    }

    public enum UnaryOperationType {
        Plus,
        Minus,
        Not,
        AddressOf,
        Dereference,
    }
}
