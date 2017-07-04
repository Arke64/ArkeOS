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
        private List<BasicBlockInstruction> currentInstructions;
        private List<LocalVariableLValue> localVariables;
        private BasicBlock entry;
        private BasicBlock parent;
        private BasicBlock current;

        public FunctionDeclarationVisitor(IReadOnlyDictionary<string, ulong> consts) => this.consts = consts;

        public FunctionLValue Visit(FunctionDeclarationNode node) {
            this.currentInstructions = new List<BasicBlockInstruction>();
            this.localVariables = new List<LocalVariableLValue>();
            this.entry = null;
            this.parent = null;
            this.current = null;

            this.Visit(node.StatementBlock);

            this.Push(new ReturnTerminator(this.CreateVariable()));

            return new FunctionLValue(node.Identifier, this.entry, node.ArgumentListDeclaration.Items.Select(i => i.Identifier).ToList(), this.localVariables);
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
                    var lhs = this.VisitEnsureIsLValue(n.Target);
                    var rhs = n is CompoundAssignmentStatementNode ca ? this.VisitAllowRValue(new BinaryExpressionNode(ca.Target, ca.Op, ca.Expression)) : this.VisitAllowRValue(n.Expression);

                    this.Push(new BasicBlockAssignmentInstruction(lhs, rhs));

                    break;

                case IfStatementNode n: {
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
                    }

                    break;

                case WhileStatementNode n: {
                        var bodyGoto = new GotoTerminator();
                        var endGoto = new GotoTerminator();
                        var entry = this.entry;
                        var parent = this.parent;
                        var insts = this.currentInstructions.Select(i => i).ToList();

                        this.currentInstructions = new List<BasicBlockInstruction>();

                        this.entry = null;
                        this.parent = null;
                        this.Visit(n.StatementBlock);
                        this.Push(bodyGoto);
                        var bodyEntry = this.entry;

                        this.entry = null;
                        this.parent = null;
                        this.Push(endGoto);
                        var endEntry = this.entry;

                        this.entry = entry;
                        this.parent = parent;
                        this.currentInstructions = insts;

                        var preconditionTerminator = new GotoTerminator();
                        this.Push(preconditionTerminator);

                        this.Push(new IfTerminator(this.Visit(n.Expression), bodyEntry, endEntry));

                        preconditionTerminator.SetTarget(this.parent);
                        bodyGoto.SetTarget(this.parent);
                    }

                    break;
            }
        }

        private void Visit(DeclarationNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "identifier node");
                case LocalVariableDeclarationWithInitializerNode n:
                    var i = this.VisitAllowRValue(n.Initializer);

                    this.Push(new BasicBlockAssignmentInstruction(new LocalVariableLValue(n.Identifier), i));

                    break;
            }
        }

        private LValue VisitEnsureIsLValue(ExpressionStatementNode node) => this.VisitAllowRValue(node) is LValue l ? l : throw new ExpectedException(default(PositionInfo), "lvalue");

        private RValue Visit(ExpressionStatementNode node) {
            var res = this.VisitAllowRValue(node);

            switch (node) {
                case IdentifierExpressionNode _:
                case LiteralExpressionNode _:
                    return res;

                default:
                    return this.CreateVariableAndAssign(res);
            }
        }

        private RValue VisitAllowRValue(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(default(PositionInfo), "expression node");
                case IdentifierExpressionNode n: return this.Visit(n);
                case LiteralExpressionNode n: return this.Visit(n);

                case BinaryExpressionNode n:
                    var l = this.Visit(n.Left);
                    var r = this.Visit(n.Right);

                    return new BinaryOperation(l, (BinaryOperationType)n.Op.Operator, r);

                case UnaryExpressionNode n:
                    if (n.Op.Operator != Operator.Dereference) {
                        return new UnaryOperation((UnaryOperationType)n.Op.Operator, this.Visit(n.Expression));
                    }
                    else {
                        return new PointerLValue(this.VisitEnsureIsLValue(n.Expression));
                    }
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

                    this.Push(new FunctionCallTerminator(n.Identifier, returnTarget, args));

                    return returnTarget;
            }
        }

        private RValue Visit(LiteralExpressionNode node) {
            switch (node) {
                case IntegerLiteralNode n: return new UnsignedIntegerConstant(n.Literal);
                case BoolLiteralNode n: return new UnsignedIntegerConstant(n.Literal ? ulong.MaxValue : 0);
                default: throw new UnexpectedException(default(PositionInfo), "literal node");
            }
        }

        private void Visit(IntrinsicStatementNode node) {
            var def = default(InstructionDefinition);

            switch (node) {
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
                default: throw new UnexpectedException(default(PositionInfo), "intrinsic node");
            }

            if (def.ParameterCount != node.ArgumentList.Items.Count) throw new TooFewArgumentsException(default(PositionInfo));

            RValue a = null, b = null, c = null;

            if (def.ParameterCount > 0) { node.ArgumentList.Extract(0, out var arg); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.Extract(1, out var arg); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.Extract(2, out var arg); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.VisitEnsureIsLValue(arg) : this.Visit(arg); }

            this.Push(new BasicBlockIntrinsicInstruction(def, a, b, c));
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
                    if (i.WhenTrue.Terminator is GotoTerminator g1 && g1.Target == null) {
                        g1.SetTarget(this.current);
                    }

                    if (i.WhenFalse.Terminator is GotoTerminator g2 && g2.Target == null) {
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
        public LValue Reference { get; }

        public PointerLValue(LValue reference) => this.Reference = reference;

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
        public BasicBlock Target { get; private set; }

        public void SetTarget(BasicBlock target) => this.Target = target;

        public override string ToString() => $"goto {this.Target}";
    }

    public sealed class IfTerminator : Terminator {
        public RValue Condition { get; }
        public BasicBlock WhenTrue { get; }
        public BasicBlock WhenFalse { get; }

        public IfTerminator(RValue condition, BasicBlock whenTrue, BasicBlock whenFalse) => (this.Condition, this.WhenTrue, this.WhenFalse) = (condition, whenTrue, whenFalse);

        public override string ToString() => $"if {this.Condition}";
    }

    public sealed class FunctionCallTerminator : Terminator {
        public string ToCall { get; }
        public LValue ReturnTarget { get; }
        public IReadOnlyCollection<FunctionArgumentLValue> Arguments { get; }
        public BasicBlock AfterReturn { get; private set; }

        public void SetAfterReturn(BasicBlock afterReturn) => this.AfterReturn = afterReturn;

        public FunctionCallTerminator(string toCall, LValue returnTarget, IReadOnlyCollection<FunctionArgumentLValue> arguments) => (this.ToCall, this.ReturnTarget, this.Arguments) = (toCall, returnTarget, arguments);

        public override string ToString() => $"{this.ReturnTarget} = call {this.ToCall}";
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
