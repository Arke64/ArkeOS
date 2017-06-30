using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    public class Lowerer {
        private Lowerer() { }

        public static ProgramDeclarationNode Lower(ProgramDeclarationNode root) {
            var nameGenerator = new NameGenerator();
            var result = new ProgramDeclarationNode();

            foreach (var i in root.ConstDeclarations.Items)
                result.ConstDeclarations.Add(i);

            foreach (var i in root.VariableDeclarations.Items)
                result.VariableDeclarations.Add(i);

            foreach (var i in root.FunctionDeclarations.Items)
                result.FunctionDeclarations.Add(FunctionDeclarationVisitor.Visit(nameGenerator, i));

            return result;
        }

        private class NameGenerator {
            private ulong nextId = 0;

            public string Next() => "_" + this.nextId++.ToString();
        }

        private class FunctionDeclarationVisitor {
            private NameGenerator nameGenerator;
            private StatementBlockNode result;

            private FunctionDeclarationVisitor(NameGenerator nameGenerator) => (this.nameGenerator, this.result) = (nameGenerator, new StatementBlockNode());

            public static FunctionDeclarationNode Visit(NameGenerator nameGenerator, FunctionDeclarationNode orig) {
                var v = new FunctionDeclarationVisitor(nameGenerator);

                v.Visit(orig.StatementBlock);

                return new FunctionDeclarationNode(orig.Token, orig.ArgumentListDeclaration, v.result);
            }

            private VariableIdentifierNode CreateAssignment(ExpressionStatementNode rhs) {
                var id = this.nameGenerator.Next();
                var ident = new VariableIdentifierNode(id);

                this.result.VariableDeclarations.Add(new VariableDeclarationNode(id));
                this.result.Statements.Add(new AssignmentStatementNode(ident, rhs));

                return ident;
            }

            private void Visit(StatementBlockNode node) {
                foreach (var i in node.VariableDeclarations.Items)
                    this.result.VariableDeclarations.Add(i);

                foreach (var b in node.Statements.Items)
                    this.Visit(b);
            }

            private void Visit(StatementNode node) {
                switch (node) {
                    case AssignmentStatementNode n:
                        var rhs = this.Visit(n.Target);
                        var lhs = this.Visit(n.Expression);

                        this.result.Statements.Add(new AssignmentStatementNode(rhs, lhs));

                        break;
                }
            }

            private IdentifierExpressionNode Visit(ExpressionStatementNode node) {
                switch (node) {
                    case VariableIdentifierNode n: return n;
                    case RegisterIdentifierNode n: return n;
                    case IntegerLiteralNode n: return this.CreateAssignment(n);
                    case BoolLiteralNode n: return this.CreateAssignment(n);

                    case BinaryExpressionNode n:
                        var l = this.Visit(n.Left);
                        var r = this.Visit(n.Right);

                        return this.CreateAssignment(new BinaryExpressionNode(l, n.Op, r));

                    case UnaryExpressionNode n:
                        var e = this.Visit(n.Expression);

                        return this.CreateAssignment(new UnaryExpressionNode(n.Op, e));
                }

                Debug.Assert(false);

                return null;
            }
        }
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
                        var (blk, res) = this.Flatten(n.Expression);

                        if (blk != null)
                            this.Visit(blk);

                        this.Push(new BasicBlockAssignmentInstruction(this.ExtractLValue(n.Target), res));

                        break;

                    case FunctionCallIdentifierNode n:

                        break;

                    default: Debug.Assert(false); break;
                }
            }
        }

        private LValue ExtractLValue(ExpressionStatementNode e) {
            switch (e) {
                case RegisterIdentifierNode n: return new RegisterVariable(n.Register);
                default: Debug.Assert(false); return null;
            }
        }

        private (StatementBlockNode, RValue) Flatten(ExpressionStatementNode esn) {
            var insts = new List<AssignmentStatementNode>();

            switch (esn) {
                case IntegerLiteralNode n: return (null, new UnsignedIntegerConstant(n.Literal));
                default: throw new NotImplementedException();
            }
        }

        private void Visit(ExpressionStatementNode expr, List<AssignmentStatementNode> instructions) {

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
