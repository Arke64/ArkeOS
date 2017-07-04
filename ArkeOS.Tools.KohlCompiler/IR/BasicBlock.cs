using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
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
            var visitor = new FunctionDeclarationVisitor(this.ast.ConstDeclarations.Items.ToDictionary(c => c.Identifier, c => c.Value.Literal));
            var functions = new List<FunctionLValue>();
            var globalVars = new List<GlobalVariableLValue>();

            foreach (var i in this.ast.VariableDeclarations.Items)
                globalVars.Add(new GlobalVariableLValue(i.Identifier));

            foreach (var i in this.ast.FunctionDeclarations.Items)
                functions.Add(visitor.Visit(i));

            return new Compiliation(functions, globalVars);
        }
    }

    public sealed class FunctionDeclarationVisitor {
        private readonly NameGenerator nameGenerator = new NameGenerator();
        private readonly IReadOnlyDictionary<string, ulong> consts;
        private List<LocalVariableLValue> localVariables;
        private BasicBlock currentBlock;

        public FunctionDeclarationVisitor(IReadOnlyDictionary<string, ulong> consts) => this.consts = consts;

        public FunctionLValue Visit(FunctionDeclarationNode node) {
            this.localVariables = new List<LocalVariableLValue>();

            var entry = this.PushBasicBlock(new BasicBlock());

            this.Visit(node.StatementBlock);

            //See if this check can be put inside pushterminator
            if (this.currentBlock.Terminator == null)
                this.PushTerminator(new ReturnTerminator(this.CreateVariable()));

            return new FunctionLValue(node.Identifier, entry, node.ArgumentListDeclaration.Items.Select(i => i.Identifier).ToList(), this.localVariables);
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
                case ReturnStatementNode n: this.PushTerminator(new ReturnTerminator(this.Visit(n.Expression))); break;
                case ExpressionStatementNode n: this.Visit(n); break;
                case IntrinsicStatementNode n: this.Visit(n); break;

                case AssignmentStatementNode n:
                    var lhs = this.VisitEnsureIsLValue(n.Target);
                    var rhs = n is CompoundAssignmentStatementNode ca ? this.VisitAllowRawOp(new BinaryExpressionNode(ca.Target, ca.Op, ca.Expression)) : this.VisitAllowRawOp(n.Expression);

                    this.PushInstuction(new BasicBlockAssignmentInstruction(lhs, rhs));

                    break;

                case IfStatementNode n: {
                        var ifTerminator = this.PushTerminator(new IfTerminator(this.Visit(n.Expression)));
                        var after = new BasicBlock();

                        var ifEntry = this.PushBasicBlock(new BasicBlock());
                        this.Visit(n.StatementBlock);
                        if (this.currentBlock.Terminator == null)
                            this.PushTerminator(new GotoTerminator(after));

                        //Similar to while, if not an IfElse, we can probably push `after` instead of `elseEntry`
                        var elseEntry = this.PushBasicBlock(new BasicBlock());
                        if (n is IfElseStatementNode ie)
                            this.Visit(ie.ElseStatementBlock);
                        if (this.currentBlock.Terminator == null)
                            this.PushTerminator(new GotoTerminator(after));

                        ifTerminator.SetNext(ifEntry, elseEntry);

                        this.PushBasicBlock(after);
                    }

                    break;

                case WhileStatementNode n: {
                        var loopEntry = new BasicBlock();
                        this.PushTerminator(new GotoTerminator(loopEntry));
                        this.PushBasicBlock(loopEntry);

                        var ifTerminator = this.PushTerminator(new IfTerminator(this.Visit(n.Expression)));
                        var after = new BasicBlock();

                        var ifEntry = this.PushBasicBlock(new BasicBlock());
                        this.Visit(n.StatementBlock);
                        this.PushTerminator(new GotoTerminator(loopEntry));

                        //We can probably elide this entire elseEntry and just push `after` instead of `elseEntry`
                        var elseEntry = this.PushBasicBlock(new BasicBlock());
                        this.PushTerminator(new GotoTerminator(after));

                        ifTerminator.SetNext(ifEntry, elseEntry);

                        this.PushBasicBlock(after);
                    }

                    break;
            }
        }

        private void Visit(DeclarationNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case LocalVariableDeclarationWithInitializerNode n:
                    var lhs = new LocalVariableLValue(n.Identifier);
                    var rhs = this.VisitAllowRawOp(n.Initializer);

                    this.PushInstuction(new BasicBlockAssignmentInstruction(lhs, rhs));

                    break;
            }
        }

        private LValue VisitEnsureIsLValue(ExpressionStatementNode node) => this.VisitAllowRawOp(node) is LValue l ? l : throw new ExpectedException(default(PositionInfo), "lvalue");

        private RValue Visit(ExpressionStatementNode node) {
            var res = this.VisitAllowRawOp(node);

            switch (node) {
                case IdentifierExpressionNode _:
                case LiteralExpressionNode _:
                    return res;

                default:
                    var ident = this.CreateVariable();

                    this.PushInstuction(new BasicBlockAssignmentInstruction(ident, res));

                    return ident;
            }
        }

        private RValue VisitAllowRawOp(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "expression node");
                case IdentifierExpressionNode n: return this.Visit(n);
                case LiteralExpressionNode n: return this.Visit(n);

                case BinaryExpressionNode n:
                    var l = this.Visit(n.Left);
                    var r = this.Visit(n.Right);

                    return new BinaryOperation(l, (BinaryOperationType)n.Op.Operator, r);

                case UnaryExpressionNode n:
                    var v = this.Visit(n.Expression);

                    return n.Op.Operator != Operator.Dereference ? new UnaryOperation((UnaryOperationType)n.Op.Operator, v) : (RValue)new PointerLValue(v);
            }
        }

        private RValue Visit(IdentifierExpressionNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case VariableIdentifierNode n: return this.consts.TryGetValue(node.Identifier, out var c) ? new UnsignedIntegerConstant(c) : (RValue)new LocalVariableLValue(n.Identifier);
                case RegisterIdentifierNode n: return new RegisterLValue(n.Register);

                case FunctionCallIdentifierNode n:
                    var returnTarget = this.CreateVariable();
                    var args = new List<FunctionArgumentLValue>();

                    foreach (var a in n.ArgumentList.Items)
                        args.Add(new FunctionArgumentLValue(this.Visit(a)));

                    var terminator = this.PushTerminator(new CallTerminator(n.Identifier, returnTarget, args));
                    var block = this.PushBasicBlock(new BasicBlock());
                    terminator.SetNext(block);

                    return returnTarget;
            }
        }

        private RValue Visit(LiteralExpressionNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "literal node");
                case IntegerLiteralNode n: return new UnsignedIntegerConstant(n.Literal);
                case BoolLiteralNode n: return new UnsignedIntegerConstant(n.Literal ? ulong.MaxValue : 0);
            }
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

            if (def.ParameterCount > 0) { node.ArgumentList.Extract(0, out var arg); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.Extract(1, out var arg); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.Extract(2, out var arg); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }

            this.PushInstuction(new BasicBlockIntrinsicInstruction(def, a, b, c));
        }

        private LocalVariableLValue CreateVariable() {
            var ident = new LocalVariableLValue(this.nameGenerator.Next());

            this.localVariables.Add(ident);

            return ident;
        }

        private BasicBlock PushBasicBlock(BasicBlock basicBlock) => this.currentBlock = basicBlock;
        private T PushTerminator<T>(T terminator) where T : Terminator { this.currentBlock.Terminator = terminator; return terminator; }
        private void PushInstuction(BasicBlockInstruction bbi) => this.currentBlock.Instructions.Add(bbi);
    }

    public sealed class BasicBlock {
        public ICollection<BasicBlockInstruction> Instructions { get; } = new List<BasicBlockInstruction>();
        public Terminator Terminator { get; set; }
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
        public IReadOnlyCollection<FunctionLValue> Functions { get; }
        public IReadOnlyCollection<GlobalVariableLValue> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<FunctionLValue> functions, IReadOnlyCollection<GlobalVariableLValue> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
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

    public sealed class FunctionArgumentLValue : LValue {
        public RValue Argument { get; }

        public FunctionArgumentLValue(RValue argument) => this.Argument = argument;

        public override string ToString() => $"arg {this.Argument.ToString()}";
    }

    public sealed class PointerLValue : LValue {
        public RValue Reference { get; }

        public PointerLValue(RValue reference) => this.Reference = reference;

        public override string ToString() => $"*({this.Reference.ToString()})";
    }

    public sealed class FunctionLValue : LValue {
        public string Identifier { get; }
        public BasicBlock Entry { get; }
        public IReadOnlyCollection<string> Arguments { get; }
        public IReadOnlyCollection<LocalVariableLValue> LocalVariables { get; }

        public FunctionLValue(string identifier, BasicBlock entry, IReadOnlyCollection<string> arguments, IReadOnlyCollection<LocalVariableLValue> variables) => (this.Identifier, this.Entry, this.Arguments, this.LocalVariables) = (identifier, entry, arguments, variables);

        public override string ToString() => $"func {this.Identifier}";
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

    public sealed class BinaryOperation : RValue {
        public RValue Left { get; }
        public BinaryOperationType Op { get; }
        public RValue Right { get; }

        public BinaryOperation(RValue left, BinaryOperationType op, RValue right) => (this.Left, this.Op, this.Right) = (left, op, right);

        public override string ToString() => $"{this.Left} '{this.Op}' {this.Right}";
    }

    public sealed class UnaryOperation : RValue {
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
        public BasicBlock Next { get; }

        public GotoTerminator(BasicBlock next) => this.Next = next;

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
        public IReadOnlyCollection<FunctionArgumentLValue> Arguments { get; }
        public BasicBlock Next { get; private set; }

        public CallTerminator(string target, LValue result, IReadOnlyCollection<FunctionArgumentLValue> arguments) => (this.Target, this.Result, this.Arguments) = (target, result, arguments);

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
