using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class NameGenerator {
        private ulong nextId = 0;

        public string Next() => "$tmp_" + this.nextId++.ToString();
    }

    public sealed class IrGenerator {
        private readonly ProgramDeclarationNode ast;

        public IrGenerator(ProgramDeclarationNode ast) => this.ast = ast;

        public Compiliation Generate() {
            var globalVars = new List<GlobalVariableLValue>();
            var functions = new List<Function>();
            var nameGenerator = new NameGenerator();
            var consts = this.ast.ConstDeclarations.Items.ToDictionary(c => c.Identifier, c => c.Value.Literal);

            foreach (var i in this.ast.VariableDeclarations.Items)
                globalVars.Add(new GlobalVariableLValue(i.Identifier));

            foreach (var i in this.ast.FunctionDeclarations.Items)
                functions.Add(FunctionDeclarationVisitor.Visit(nameGenerator, consts, i));

            return new Compiliation(functions, globalVars);
        }
    }

    public sealed class BasicBlockCreator {
        private BasicBlock currentBlock;

        public BasicBlock Entry { get; } = new BasicBlock();

        public BasicBlockCreator() => this.currentBlock = this.Entry;

        private BasicBlock SetBlock(BasicBlock block) => this.currentBlock = block;
        private T SetTerminator<T>(T terminator) where T : Terminator => this.currentBlock.Terminator == null ? (T)(this.currentBlock.Terminator = terminator) : throw new InvalidOperationException();

        public BasicBlock PushNew() => this.SetBlock(new BasicBlock());
        public BasicBlock PushNew(BasicBlock block) => this.SetBlock(block);

        public (T, BasicBlock) PushTerminator<T>(T terminator) where T : Terminator => (this.SetTerminator(terminator), this.PushNew());
        public (T, BasicBlock) PushTerminator<T>(T terminator, BasicBlock next) where T : Terminator => (this.SetTerminator(terminator), this.PushNew(next));

        public void PushInstuction(BasicBlockInstruction bbi) => this.currentBlock.Instructions.Add(bbi);
    }

    public sealed class FunctionDeclarationVisitor {
        private readonly List<LocalVariableLValue> localVariables = new List<LocalVariableLValue>();
        private readonly BasicBlockCreator block = new BasicBlockCreator();
        private readonly IReadOnlyDictionary<string, ulong> consts;
        private readonly NameGenerator nameGenerator;

        public static Function Visit(NameGenerator nameGenerator, IReadOnlyDictionary<string, ulong> consts, FunctionDeclarationNode node) => new FunctionDeclarationVisitor(nameGenerator, consts).Visit(node);

        private FunctionDeclarationVisitor(NameGenerator nameGenerator, IReadOnlyDictionary<string, ulong> consts) => (this.nameGenerator, this.consts) = (nameGenerator, consts);

        private LocalVariableLValue CreateTemporaryLocalVariable() {
            var ident = new LocalVariableLValue(this.nameGenerator.Next());

            this.localVariables.Add(ident);

            return ident;
        }

        private Function Visit(FunctionDeclarationNode node) {
            this.Visit(node.StatementBlock);

            this.block.PushTerminator(new ReturnTerminator(this.CreateTemporaryLocalVariable()));

            return new Function(node.Identifier, this.block.Entry, node.ArgumentListDeclaration.Items.Select(i => i.Identifier).ToList(), this.localVariables);
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
                case ReturnStatementNode n: this.Visit(n); break;
                case AssignmentStatementNode n: this.Visit(n); break;
                case IfStatementNode n: this.Visit(n); break;
                case WhileStatementNode n: this.Visit(n); break;
                case IntrinsicStatementNode n: this.Visit(n); break;
                case ExpressionStatementNode n: this.Visit(n); break;
            }
        }

        private void Visit(DeclarationNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case LocalVariableDeclarationWithInitializerNode n: this.block.PushInstuction(new BasicBlockAssignmentInstruction(new LocalVariableLValue(n.Identifier), this.ExtractRValueOrOp(n.Initializer))); break;
            }
        }

        private void Visit(ReturnStatementNode node) {
            this.block.PushTerminator(new ReturnTerminator(this.ExtractRValue(node.Expression)));
        }

        private void Visit(AssignmentStatementNode node) {
            var lhs = this.ExtractLValue(node.Target);
            var exp = node.Expression;

            if (node is CompoundAssignmentStatementNode cnode)
                exp = new BinaryExpressionNode(cnode.Target, cnode.Op, cnode.Expression);

            this.block.PushInstuction(new BasicBlockAssignmentInstruction(lhs, this.ExtractRValueOrOp(exp)));
        }

        private void Visit(IfStatementNode node) {
            var (startTerminator, ifBlock) = this.block.PushTerminator(new IfTerminator(this.ExtractRValue(node.Expression)));
            this.Visit(node.StatementBlock);
            var (ifTerminator, endBlock) = this.block.PushTerminator(new GotoTerminator());
            ifTerminator.SetNext(endBlock);

            var elseBlock = endBlock;
            if (node is IfElseStatementNode elseNode) {
                elseBlock = this.block.PushNew();
                this.Visit(elseNode.ElseStatementBlock);
                var (elseTerminator, _) = this.block.PushTerminator(new GotoTerminator(), endBlock);
                elseTerminator.SetNext(endBlock);
            }

            startTerminator.SetNext(ifBlock, elseBlock);
        }

        private void Visit(WhileStatementNode node) {
            var (startTerminator, conditionBlock) = this.block.PushTerminator(new GotoTerminator());
            startTerminator.SetNext(conditionBlock);

            var (conditionTerminator, loopBlock) = this.block.PushTerminator(new IfTerminator(this.ExtractRValue(node.Expression)));
            this.Visit(node.StatementBlock);

            var (loopTerminator, endBlock) = this.block.PushTerminator(new GotoTerminator());
            loopTerminator.SetNext(conditionBlock);

            conditionTerminator.SetNext(loopBlock, endBlock);
        }

        private void Visit(IntrinsicStatementNode node) {
            var def = default(InstructionDefinition);

            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "intrinsic node");
                case BrkStatementNode n: def = InstructionDefinition.BRK; break;
                case EintStatementNode n: def = InstructionDefinition.EINT; break;
                case HltStatementNode n: def = InstructionDefinition.HLT; break;
                case IntdStatementNode n: def = InstructionDefinition.INTD; break;
                case InteStatementNode n: def = InstructionDefinition.INTE; break;
                case NopStatementNode n: def = InstructionDefinition.NOP; break;
                case CpyStatementNode n: def = InstructionDefinition.CPY; break;
                case IntStatementNode n: def = InstructionDefinition.INT; break;
                case DbgStatementNode n: def = InstructionDefinition.DBG; break;
                case CasStatementNode n: def = InstructionDefinition.CAS; break;
                case XchgStatementNode n: def = InstructionDefinition.XCHG; break;
            }

            if (def.ParameterCount != node.ArgumentList.Items.Count) throw new TooFewArgumentsException(default(PositionInfo));

            RValue a = null, b = null, c = null;

            if (def.ParameterCount > 0) { node.ArgumentList.Extract(0, out var arg); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.Extract(1, out var arg); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.Extract(2, out var arg); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }

            this.block.PushInstuction(new BasicBlockIntrinsicInstruction(def, a, b, c));
        }

        private void Visit(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "expression node");
                case FunctionCallIdentifierNode n: this.Visit(n, this.CreateTemporaryLocalVariable()); break;
            }
        }

        private void Visit(FunctionCallIdentifierNode node, LValue result) {
            var args = node.ArgumentList.Items.Select(a => this.ExtractRValue(a)).ToList();

            var (callTerminator, returnBlock) = this.block.PushTerminator(new CallTerminator(node.Identifier, result, args));
            callTerminator.SetNext(returnBlock);
        }

        private RValue ExtractRValue(ExpressionStatementNode node) {
            var res = this.ExtractRValueOrOp(node);

            if (res is RValue r)
                return r;

            var ident = this.CreateTemporaryLocalVariable();

            this.block.PushInstuction(new BasicBlockAssignmentInstruction(ident, res));

            return ident;
        }

        private LValue ExtractLValue(ExpressionStatementNode node) {
            var res = this.ExtractRValueOrOp(node);

            if (res is LValue l)
                return l;

            throw new ExpectedException(default(PositionInfo), "lvalue");
        }

        private RValueOrOp ExtractRValueOrOp(ExpressionStatementNode node) {
            switch (node) {
                case LiteralExpressionNode n:
                    switch (n) {
                        case IntegerLiteralNode l: return new UnsignedIntegerConstant(l.Literal);
                        case BoolLiteralNode l: return new UnsignedIntegerConstant(l.Literal ? ulong.MaxValue : 0);
                    }

                    break;

                case IdentifierExpressionNode n:
                    switch (n) {
                        case VariableIdentifierNode i when this.consts.TryGetValue(i.Identifier, out var c): return new UnsignedIntegerConstant(c);
                        case VariableIdentifierNode i: return new LocalVariableLValue(i.Identifier);
                        case RegisterIdentifierNode i: return new RegisterLValue(i.Register);
                        case FunctionCallIdentifierNode i: var ident = this.CreateTemporaryLocalVariable(); this.Visit(i, ident); return ident;
                    }

                    break;

                case BinaryExpressionNode n:
                    var lhs = this.ExtractRValue(n.Left);
                    var rhs = this.ExtractRValue(n.Right);

                    return new BinaryOperation(lhs, (BinaryOperationType)n.Op.Operator, rhs);

                case UnaryExpressionNode n when n.Op.Operator != Operator.Dereference: return new UnaryOperation((UnaryOperationType)n.Op.Operator, this.ExtractRValue(n.Expression));
                case UnaryExpressionNode n when n.Op.Operator == Operator.Dereference: return new PointerLValue(this.ExtractRValue(n.Expression));
            }

            throw new UnexpectedException(default(PositionInfo), "expression node");
        }
    }

    public sealed class BasicBlock {
        public ICollection<BasicBlockInstruction> Instructions { get; } = new List<BasicBlockInstruction>();
        public Terminator Terminator { get; set; }
    }

    public abstract class BasicBlockInstruction {

    }

    public sealed class BasicBlockAssignmentInstruction : BasicBlockInstruction {
        public LValue Target { get; }
        public RValueOrOp Value { get; }

        public BasicBlockAssignmentInstruction(LValue target, RValueOrOp value) => (this.Target, this.Value) = (target, value);

        public override string ToString() => $"{this.Target} = {this.Value}";
    }

    public sealed class BasicBlockIntrinsicInstruction : BasicBlockInstruction {
        public InstructionDefinition Intrinsic { get; }
        public RValue Argument1 { get; }
        public RValue Argument2 { get; }
        public RValue Argument3 { get; }

        public BasicBlockIntrinsicInstruction(InstructionDefinition inst) : this(inst, null, null, null) { }
        public BasicBlockIntrinsicInstruction(InstructionDefinition inst, RValue argument1) : this(inst, argument1, null, null) { }
        public BasicBlockIntrinsicInstruction(InstructionDefinition inst, RValue argument1, RValue argument2) : this(inst, argument1, argument2, null) { }
        public BasicBlockIntrinsicInstruction(InstructionDefinition inst, RValue argument1, RValue argument2, RValue argument3) => (this.Intrinsic, this.Argument1, this.Argument2, this.Argument3) = (inst, argument1, argument2, argument3);

        public override string ToString() => $"{this.Intrinsic.Mnemonic} {this.Argument1} {this.Argument2} {this.Argument3}";
    }

    public sealed class Compiliation {
        public IReadOnlyCollection<Function> Functions { get; }
        public IReadOnlyCollection<GlobalVariableLValue> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariableLValue> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public sealed class Function {
        public string Identifier { get; }
        public BasicBlock Entry { get; }
        public IReadOnlyCollection<string> Arguments { get; }
        public IReadOnlyCollection<LocalVariableLValue> LocalVariables { get; }

        public Function(string identifier, BasicBlock entry, IReadOnlyCollection<string> arguments, IReadOnlyCollection<LocalVariableLValue> variables) => (this.Identifier, this.Entry, this.Arguments, this.LocalVariables) = (identifier, entry, arguments, variables);

        public override string ToString() => $"func {this.Identifier}";
    }

    public abstract class LValue : RValue {

    }

    public sealed class GlobalVariableLValue : LValue {
        public string Identifier { get; }

        public GlobalVariableLValue(string identifier) => this.Identifier = identifier;

        public override string ToString() => $"gbl var {this.Identifier}";
    }

    public sealed class LocalVariableLValue : LValue {
        public string Identifier { get; }

        public LocalVariableLValue(string identifier) => this.Identifier = identifier;

        public override string ToString() => $"var {this.Identifier}";
    }

    public sealed class RegisterLValue : LValue {
        public Register Register { get; }

        public RegisterLValue(Register register) => this.Register = register;

        public override string ToString() => this.Register.ToString();
    }

    public sealed class PointerLValue : LValue {
        public RValue Reference { get; }

        public PointerLValue(RValue reference) => this.Reference = reference;

        public override string ToString() => $"*({this.Reference.ToString()})";
    }

    public abstract class RValueOrOp {

    }

    public abstract class RValue : RValueOrOp {

    }

    public abstract class Constant : RValue {

    }

    public sealed class UnsignedIntegerConstant : Constant {
        public ulong Value { get; }

        public UnsignedIntegerConstant(ulong value) => this.Value = value;

        public override string ToString() => this.Value.ToString();
    }

    public sealed class BinaryOperation : RValueOrOp {
        public RValue Left { get; }
        public BinaryOperationType Op { get; }
        public RValue Right { get; }

        public BinaryOperation(RValue left, BinaryOperationType op, RValue right) => (this.Left, this.Op, this.Right) = (left, op, right);

        public override string ToString() => $"{this.Left} '{this.Op}' {this.Right}";
    }

    public sealed class UnaryOperation : RValueOrOp {
        public UnaryOperationType Op { get; }
        public RValue Value { get; }

        public UnaryOperation(UnaryOperationType op, RValue value) => (this.Op, this.Value) = (op, value);

        public override string ToString() => $"'{this.Op}' {this.Value}";
    }

    public abstract class Terminator {

    }

    public sealed class ReturnTerminator : Terminator {
        public RValue Value { get; }

        public ReturnTerminator(RValue value) => this.Value = value;

        public override string ToString() => $"return {this.Value}";
    }

    public sealed class GotoTerminator : Terminator {
        public BasicBlock Next { get; private set; }

        public void SetNext(BasicBlock next) => this.Next = next;

        public override string ToString() => $"goto {this.Next}";
    }

    public sealed class IfTerminator : Terminator {
        public RValue Condition { get; }
        public BasicBlock NextTrue { get; private set; }
        public BasicBlock NextFalse { get; private set; }

        public IfTerminator(RValue condition) => this.Condition = condition;

        public void SetNext(BasicBlock nextTrue, BasicBlock nextFalse) => (this.NextTrue, this.NextFalse) = (nextTrue, nextFalse);

        public override string ToString() => $"if {this.Condition}";
    }

    public sealed class CallTerminator : Terminator {
        public string Target { get; }
        public LValue Result { get; }
        public IReadOnlyCollection<RValue> Arguments { get; }
        public BasicBlock Next { get; private set; }

        public CallTerminator(string target, LValue result, IReadOnlyCollection<RValue> arguments) => (this.Target, this.Result, this.Arguments) = (target, result, arguments);

        public void SetNext(BasicBlock next) => this.Next = next;

        public override string ToString() => $"{this.Result} = call {this.Target}";
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
        Plus = Operator.UnaryPlus,
        Minus = Operator.UnaryMinus,
        Not = Operator.Not,
        AddressOf = Operator.AddressOf,
        Dereference = Operator.Dereference,
    }
}
