using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public class SymbolTable {
        private ulong nextTemporarySymbolId = 0;

        public IReadOnlyCollection<ConstVariableSymbol> ConstVariables { get; }
        public IReadOnlyCollection<GlobalVariableSymbol> GlobalVariables { get; }
        public IReadOnlyCollection<RegisterSymbol> Registers { get; }
        public IReadOnlyCollection<FunctionSymbol> Functions { get; }

        public SymbolTable(ProgramDeclarationNode ast) {
            this.ConstVariables = ast.ConstDeclarations.Items.Select(i => new ConstVariableSymbol(i.Identifier, i.Value.Literal)).ToList();
            this.GlobalVariables = ast.VariableDeclarations.Items.Select(i => new GlobalVariableSymbol(i.Identifier)).ToList();
            this.Registers = EnumExtensions.ToList<Register>().Select(i => new RegisterSymbol(i.ToString(), i)).ToList();
            this.Functions = ast.FunctionDeclarations.Items.Select(i => this.Visit(i)).ToList();
        }

        private FunctionSymbol Visit(FunctionDeclarationNode node) {
            var variables = new List<LocalVariableSymbol>();

            void visitStatementBlock(StatementBlockNode n)
            {
                variables.AddRange(n.VariableDeclarations.Items.Select(i => new LocalVariableSymbol(i.Identifier)));

                foreach (var s in n.Statements.Items)
                    visitStatement(s);
            }

            void visitStatement(StatementNode n)
            {
                switch (n) {
                    case IfElseStatementNode s: visitStatementBlock(s.ElseStatementBlock); visitStatementBlock(s.StatementBlock); break;
                    case IfStatementNode s: visitStatementBlock(s.StatementBlock); break;
                    case WhileStatementNode s: visitStatementBlock(s.StatementBlock); break;
                }
            }

            visitStatementBlock(node.StatementBlock);

            return new FunctionSymbol(node.Identifier, node.ArgumentListDeclaration.Items.Select(i => new ArgumentSymbol(i.Identifier)).ToList(), variables);
        }

        public LocalVariableSymbol CreateTemporaryLocalVariable(FunctionSymbol function) {
            var variable = new LocalVariableSymbol("$tmp_" + this.nextTemporarySymbolId++.ToString());

            function.AddLocalVariable(variable);

            return variable;
        }

        private bool TryFind<T>(IReadOnlyCollection<T> collection, string name, out T result) where T : Symbol => (result = collection.SingleOrDefault(c => c.Name == name)) != null;

        public bool TryFindConstVariable(string name, out ConstVariableSymbol result) => this.TryFind(this.ConstVariables, name, out result);
        public bool TryFindGlobalVariable(string name, out GlobalVariableSymbol result) => this.TryFind(this.GlobalVariables, name, out result);
        public bool TryFindRegister(string name, out RegisterSymbol result) => this.TryFind(this.Registers, name, out result);
        public bool TryFindFunction(string name, out FunctionSymbol result) => this.TryFind(this.Functions, name, out result);
        public bool TryFindArgument(FunctionSymbol function, string name, out ArgumentSymbol result) => this.TryFind(function.Arguments, name, out result);
        public bool TryFindLocalVariable(FunctionSymbol function, string name, out LocalVariableSymbol result) => this.TryFind(function.LocalVariables, name, out result);
    }

    public abstract class Symbol {
        public string Name { get; }

        protected Symbol(string name) => this.Name = name;

        public override string ToString() => $"{this.Name}({this.GetType().Name})";
    }

    public sealed class FunctionSymbol : Symbol {
        private List<ArgumentSymbol> arguments;
        private List<LocalVariableSymbol> localVariables;

        public IReadOnlyCollection<ArgumentSymbol> Arguments => this.arguments;
        public IReadOnlyCollection<LocalVariableSymbol> LocalVariables => this.localVariables;

        public FunctionSymbol(string name, IReadOnlyCollection<ArgumentSymbol> arguments, IReadOnlyCollection<LocalVariableSymbol> variables) : base(name) => (this.arguments, this.localVariables) = (arguments.ToList(), variables.ToList());

        public void AddLocalVariable(LocalVariableSymbol variable) => this.localVariables.Add(variable);
    }

    public sealed class ArgumentSymbol : Symbol {
        public ArgumentSymbol(string name) : base(name) { }
    }

    public sealed class LocalVariableSymbol : Symbol {
        public LocalVariableSymbol(string name) : base(name) { }
    }

    public sealed class RegisterSymbol : Symbol {
        public Register Register { get; }

        public RegisterSymbol(string name, Register register) : base(name) => this.Register = register;
    }

    public sealed class GlobalVariableSymbol : Symbol {
        public GlobalVariableSymbol(string name) : base(name) { }
    }

    public sealed class ConstVariableSymbol : Symbol {
        public ulong Value { get; }

        public ConstVariableSymbol(string name, ulong value) : base(name) => this.Value = value;
    }
}

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class IrGenerator {
        private readonly ProgramDeclarationNode ast;

        public IrGenerator(ProgramDeclarationNode ast) => this.ast = ast;

        public Compiliation Generate() {
            var symbolTable = new SymbolTable(this.ast);
            var functions = this.ast.FunctionDeclarations.Items.Select(i => FunctionDeclarationVisitor.Visit(symbolTable, i)).ToList();

            return new Compiliation(functions, symbolTable.GlobalVariables);
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
        private readonly BasicBlockCreator block = new BasicBlockCreator();
        private readonly SymbolTable symbolTable;
        private readonly FunctionSymbol functionSymbol;

        private FunctionDeclarationVisitor(SymbolTable symbolTable, FunctionSymbol functionSymbol) => (this.symbolTable, this.functionSymbol) = (symbolTable, functionSymbol);

        private LocalVariableLValue CreateTemporaryLocalVariable() => new LocalVariableLValue(this.symbolTable.CreateTemporaryLocalVariable(this.functionSymbol));

        public static Function Visit(SymbolTable symbolTable, FunctionDeclarationNode node) {
            var func = symbolTable.TryFindFunction(node.Identifier, out var f) ? f : throw new IdentifierNotFoundException(node.Position, node.Identifier);
            var visitor = new FunctionDeclarationVisitor(symbolTable, func);

            visitor.Visit(node.StatementBlock);

            visitor.block.PushTerminator(new ReturnTerminator(visitor.CreateTemporaryLocalVariable()));

            return new Function(func, visitor.block.Entry);
        }

        private void Visit(StatementBlockNode node) {
            foreach (var b in node.Statements.Items)
                this.Visit(b);
        }

        private void Visit(StatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(node.Position, "statement node");
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
                default: throw new UnexpectedException(node.Position, "identifier node");
                case LocalVariableDeclarationWithInitializerNode n: this.Visit(new AssignmentStatementNode(new IdentifierNode(n.Token), n.Initializer)); break;
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

            this.block.PushInstuction(new BasicBlockAssignmentInstruction(lhs, this.ExtractRValue(exp)));
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
                default: throw new UnexpectedException(node.Position, "intrinsic node");
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

            if (def.ParameterCount != node.ArgumentList.Items.Count) throw new TooFewArgumentsException(node.Position);

            RValue a = null, b = null, c = null;

            if (def.ParameterCount > 0) { node.ArgumentList.Extract(0, out var arg); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.Extract(1, out var arg); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.Extract(2, out var arg); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }

            this.block.PushInstuction(new BasicBlockIntrinsicInstruction(def, a, b, c));
        }

        private void Visit(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(node.Position, "expression node");
                case FunctionCallIdentifierNode n: this.Visit(n); break;
            }
        }

        private LValue Visit(FunctionCallIdentifierNode node) {
            var args = node.ArgumentList.Items.Select(a => this.ExtractRValue(a)).ToList();
            var func = this.symbolTable.TryFindFunction(node.Identifier, out var f) ? f : throw new IdentifierNotFoundException(node.Position, node.Identifier);
            var result = this.CreateTemporaryLocalVariable();

            var (callTerminator, returnBlock) = this.block.PushTerminator(new CallTerminator(func, result, args));
            callTerminator.SetNext(returnBlock);

            return result;
        }

        private LValue ExtractLValue(IdentifierExpressionNode node) => (LValue)this.ExtractValue(node, false);
        private RValue ExtractRValue(IdentifierExpressionNode node) => this.ExtractValue(node, true);

        private RValue ExtractValue(IdentifierExpressionNode node, bool allowRValue) {
            if (this.symbolTable.TryFindRegister(node.Identifier, out var rs)) return new RegisterLValue(rs);
            if (this.symbolTable.TryFindArgument(this.functionSymbol, node.Identifier, out var ps)) return new ArgumentLValue(ps);
            if (this.symbolTable.TryFindLocalVariable(this.functionSymbol, node.Identifier, out var ls)) return new LocalVariableLValue(ls);
            if (this.symbolTable.TryFindGlobalVariable(node.Identifier, out var gs)) return new GlobalVariableLValue(gs);
            if (allowRValue && this.symbolTable.TryFindConstVariable(node.Identifier, out var cs)) return new IntegerRValue(cs.Value);

            throw new ExpectedException(node.Position, "lvalue");
        }

        private LValue ExtractLValue(ExpressionStatementNode node) {
            switch (node) {
                default: throw new ExpectedException(node.Position, "lvalue");
                case IdentifierExpressionNode n: return this.ExtractLValue(n);
                case UnaryExpressionNode n when n.Op.Operator == Operator.Dereference: return new PointerLValue(this.ExtractRValue(n.Expression));
            }
        }

        private RValue ExtractRValue(ExpressionStatementNode node) {
            switch (node) {
                case IntegerLiteralNode n: return new IntegerRValue(n.Literal);
                case BoolLiteralNode n: return new IntegerRValue(n.Literal ? ulong.MaxValue : 0);
                case FunctionCallIdentifierNode n: return this.Visit(n);
                case IdentifierExpressionNode n: return this.ExtractRValue(n);
                case UnaryExpressionNode n:
                    switch (n.Op.Operator) {
                        case Operator.UnaryMinus: return this.ExtractRValue(new BinaryExpressionNode(n.Expression, OperatorNode.FromOperator(Operator.Multiplication), new IntegerLiteralNode(ulong.MaxValue)));
                        case Operator.Not: return this.ExtractRValue(new BinaryExpressionNode(n.Expression, OperatorNode.FromOperator(Operator.Xor), new IntegerLiteralNode(ulong.MaxValue)));
                        case Operator.AddressOf: return new AddressOfRValue(this.ExtractLValue(n.Expression));
                        case Operator.Dereference: return new PointerLValue(this.ExtractRValue(n.Expression));
                    }

                    break;

                case BinaryExpressionNode n:
                    var target = this.CreateTemporaryLocalVariable();

                    this.block.PushInstuction(new BasicBlockBinaryOperationInstruction(target, this.ExtractRValue(n.Left), (BinaryOperationType)n.Op.Operator, this.ExtractRValue(n.Right)));

                    return target;
            }

            throw new UnexpectedException(node.Position, "expression node");
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
        public RValue Value { get; }

        public BasicBlockAssignmentInstruction(LValue target, RValue value) => (this.Target, this.Value) = (target, value);

        public override string ToString() => $"{this.Target} = {this.Value}";
    }

    public sealed class BasicBlockBinaryOperationInstruction : BasicBlockInstruction {
        public LValue Target { get; }
        public RValue Left { get; }
        public BinaryOperationType Op { get; }
        public RValue Right { get; }

        public BasicBlockBinaryOperationInstruction(LValue target, RValue left, BinaryOperationType op, RValue right) => (this.Target, this.Left, this.Op, this.Right) = (target, left, op, right);

        public override string ToString() => $"{this.Target} = {this.Left} '{this.Op}' {this.Right}";
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
        public IReadOnlyCollection<GlobalVariableSymbol> GlobalVariables { get; }

        public Compiliation(IReadOnlyCollection<Function> functions, IReadOnlyCollection<GlobalVariableSymbol> globalVars) => (this.Functions, this.GlobalVariables) = (functions, globalVars);
    }

    public sealed class Function {
        public FunctionSymbol Symbol { get; }
        public BasicBlock Entry { get; }

        public Function(FunctionSymbol symbol, BasicBlock entry) => (this.Symbol, this.Entry) = (symbol, entry);

        public override string ToString() => $"func {this.Symbol}";
    }

    public abstract class RValue {

    }

    public sealed class IntegerRValue : RValue {
        public ulong Value { get; }

        public IntegerRValue(ulong value) => this.Value = value;

        public override string ToString() => this.Value.ToString();
    }

    public sealed class AddressOfRValue : RValue {
        public LValue Target { get; }

        public AddressOfRValue(LValue target) => this.Target = target;

        public override string ToString() => $"addr {this.Target}";
    }

    public abstract class LValue : RValue {

    }

    public sealed class ArgumentLValue : LValue {
        public ArgumentSymbol Symbol { get; }

        public ArgumentLValue(ArgumentSymbol argument) => this.Symbol = argument;

        public override string ToString() => $"aref {this.Symbol}";
    }

    public sealed class LocalVariableLValue : LValue {
        public LocalVariableSymbol Symbol { get; }

        public LocalVariableLValue(LocalVariableSymbol variable) => this.Symbol = variable;

        public override string ToString() => $"vref {this.Symbol}";
    }

    public sealed class GlobalVariableLValue : LValue {
        public GlobalVariableSymbol Symbol { get; }

        public GlobalVariableLValue(GlobalVariableSymbol variable) => this.Symbol = variable;

        public override string ToString() => $"gref {this.Symbol}";
    }

    public sealed class PointerLValue : LValue {
        public RValue Target { get; }

        public PointerLValue(RValue target) => this.Target = target;

        public override string ToString() => $"pref {this.Target}";
    }

    public sealed class RegisterLValue : LValue {
        public RegisterSymbol Symbol { get; }

        public RegisterLValue(RegisterSymbol symbol) => this.Symbol = symbol;

        public override string ToString() => $"rref {this.Symbol}";
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
        public FunctionSymbol Target { get; }
        public LValue Result { get; }
        public IReadOnlyCollection<RValue> Arguments { get; }
        public BasicBlock Next { get; private set; }

        public CallTerminator(FunctionSymbol target, LValue result, IReadOnlyCollection<RValue> arguments) => (this.Target, this.Result, this.Arguments) = (target, result, arguments);

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
}
