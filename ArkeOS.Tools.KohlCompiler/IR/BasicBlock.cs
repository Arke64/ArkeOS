using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public static class WellKnownSymbol {
        public static TypeSymbol Word { get; } = new TypeSymbol("word");
        public static TypeSymbol Bool { get; } = new TypeSymbol("bool");
    }

    public sealed class SymbolTable {
        private static IReadOnlyCollection<TypeSymbol> WellKnownTypes { get; } = new List<TypeSymbol> { WellKnownSymbol.Word, WellKnownSymbol.Bool };

        private ulong nextTemporarySymbolId = 0;

        public IReadOnlyCollection<ConstVariableSymbol> ConstVariables { get; }
        public IReadOnlyCollection<GlobalVariableSymbol> GlobalVariables { get; }
        public IReadOnlyCollection<RegisterSymbol> Registers { get; }
        public IReadOnlyCollection<FunctionSymbol> Functions { get; }

        public SymbolTable(ProgramDeclarationNode ast) {
            this.ConstVariables = ast.ConstDeclarations.Select(i => new ConstVariableSymbol(i.Identifier, this.FindType(i.Type), i.Value.Literal)).ToList();
            this.GlobalVariables = ast.VariableDeclarations.Select(i => new GlobalVariableSymbol(i.Identifier, this.FindType(i.Type))).ToList();
            this.Registers = EnumExtensions.ToList<Register>().Select(i => new RegisterSymbol(i.ToString(), i)).ToList();
            this.Functions = ast.FunctionDeclarations.Select(i => this.Visit(i)).ToList();
        }

        private FunctionSymbol Visit(FunctionDeclarationNode node) {
            var variables = new List<LocalVariableSymbol>();

            void visitStatementBlock(StatementBlockNode n)
            {
                variables.AddRange(n.VariableDeclarations.Select(i => new LocalVariableSymbol(i.Identifier, this.FindType(i.Type))));

                foreach (var s in n.Statements)
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

            return new FunctionSymbol(node.Identifier, this.FindType(node.Type), node.ArgumentListDeclaration.Select(i => new ArgumentSymbol(i.Identifier, this.FindType(i.Type))).ToList(), variables);
        }

        public LocalVariableSymbol CreateTemporaryLocalVariable(FunctionSymbol function, TypeSymbol type) {
            var variable = new LocalVariableSymbol("$tmp_" + this.nextTemporarySymbolId++.ToString(), type);

            function.AddLocalVariable(variable);

            return variable;
        }

        private bool TryFind<T>(IReadOnlyCollection<T> collection, string name, out T result) where T : Symbol => (result = collection.SingleOrDefault(c => c.Name == name)) != null;

        public TypeSymbol FindType(TypeIdentifierNode node) => this.GetTypeSymbol(node);
        public ConstVariableSymbol FindConstVariable(PositionInfo position, string name) => this.TryFindConstVariable(name, out var r) ? r : throw new IdentifierNotFoundException(position, name);
        public GlobalVariableSymbol FindGlobalVariable(PositionInfo position, string name) => this.TryFindGlobalVariable(name, out var r) ? r : throw new IdentifierNotFoundException(position, name);
        public RegisterSymbol FindRegister(PositionInfo position, string name) => this.TryFindRegister(name, out var r) ? r : throw new IdentifierNotFoundException(position, name);
        public FunctionSymbol FindFunction(PositionInfo position, string name) => this.TryFindFunction(name, out var r) ? r : throw new IdentifierNotFoundException(position, name);
        public ArgumentSymbol FindArgument(PositionInfo position, FunctionSymbol function, string name) => this.TryFindArgument(function, name, out var r) ? r : throw new IdentifierNotFoundException(position, name);
        public LocalVariableSymbol FindLocalVariable(PositionInfo position, FunctionSymbol function, string name) => this.TryFindLocalVariable(function, name, out var r) ? r : throw new IdentifierNotFoundException(position, name);

        public bool TryFindType(string name, out TypeSymbol result) => this.TryFind(SymbolTable.WellKnownTypes, name, out result);
        public bool TryFindConstVariable(string name, out ConstVariableSymbol result) => this.TryFind(this.ConstVariables, name, out result);
        public bool TryFindGlobalVariable(string name, out GlobalVariableSymbol result) => this.TryFind(this.GlobalVariables, name, out result);
        public bool TryFindRegister(string name, out RegisterSymbol result) => this.TryFind(this.Registers, name, out result);
        public bool TryFindFunction(string name, out FunctionSymbol result) => this.TryFind(this.Functions, name, out result);
        public bool TryFindArgument(FunctionSymbol function, string name, out ArgumentSymbol result) => this.TryFind(function.Arguments, name, out result);
        public bool TryFindLocalVariable(FunctionSymbol function, string name, out LocalVariableSymbol result) => this.TryFind(function.LocalVariables, name, out result);

        private TypeSymbol GetTypeSymbol(TypeIdentifierNode node) {
            var count = 0;

            while (node.GenericArguments.Any()) {
                count++;
                node = node.GenericArguments.Single();
            }

            var type = this.TryFindType(node.Identifier, out var r) ? r : throw new IdentifierNotFoundException(node.Position, node.Identifier);

            while (count-- > 0)
                type = new TypeSymbol("ptr", type);

            return type;
        }

        public void CheckTypeOfExpression(ExpressionStatementNode node) => this.CheckTypeOfExpression(node, default(FunctionSymbol));
        public void CheckTypeOfExpression(ExpressionStatementNode node, FunctionSymbol function) => this.GetTypeOfExpression(node, function);

        public void CheckTypeOfExpression(TypeIdentifierNode expected, ExpressionStatementNode node) => this.CheckTypeOfExpression(expected, node, null);
        public void CheckTypeOfExpression(TypeIdentifierNode expected, ExpressionStatementNode node, FunctionSymbol function) => this.CheckTypeOfExpression(this.GetTypeSymbol(expected), node, function);

        public void CheckTypeOfExpression(ExpressionStatementNode expected, ExpressionStatementNode node) => this.CheckTypeOfExpression(expected, node, null);
        public void CheckTypeOfExpression(ExpressionStatementNode expected, ExpressionStatementNode node, FunctionSymbol function) => this.CheckTypeOfExpression(this.GetTypeOfExpression(expected, function), node, function);

        public void CheckTypeOfExpression(TypeSymbol expected, ExpressionStatementNode node) => this.CheckTypeOfExpression(expected, node, null);
        public void CheckTypeOfExpression(TypeSymbol expected, ExpressionStatementNode node, FunctionSymbol function) { var type = this.GetTypeOfExpression(node, function); if (type != expected) throw new WrongTypeException(node.Position, type.Name); }

        public TypeSymbol GetTypeOfExpression(ExpressionStatementNode node) => this.GetTypeOfExpression(node, null);

        public TypeSymbol GetTypeOfExpression(ExpressionStatementNode node, FunctionSymbol function) {
            switch (node) {
                case IntegerLiteralNode n: return WellKnownSymbol.Word;
                case BoolLiteralNode n: return WellKnownSymbol.Bool;
                case FunctionCallIdentifierNode n: return this.TryFindFunction(n.Identifier, out var f) ? f.Type : throw new IdentifierNotFoundException(n.Position, n.Identifier);
                case IdentifierExpressionNode n:
                    if (this.TryFindRegister(n.Identifier, out var rs)) return WellKnownSymbol.Word;
                    if (function != null && this.TryFindArgument(function, n.Identifier, out var ps)) return ps.Type;
                    if (function != null && this.TryFindLocalVariable(function, n.Identifier, out var ls)) return ls.Type;
                    if (this.TryFindGlobalVariable(n.Identifier, out var gs)) return gs.Type;
                    if (this.TryFindConstVariable(n.Identifier, out var cs)) return cs.Type;

                    break;

                case UnaryExpressionNode n:
                    var t = this.GetTypeOfExpression(n.Expression, function);

                    switch (n.Op.Operator) {
                        case Operator.UnaryMinus:
                            if (t != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, t.Name);

                            return t;

                        case Operator.Not:
                            return t;

                        case Operator.AddressOf:
                            return new TypeSymbol("ptr", t);

                        case Operator.Dereference:
                            return t.GenericArguments.Single();
                    }

                    break;

                case BinaryExpressionNode n:
                    var lt = this.GetTypeOfExpression(n.Left, function);
                    var rt = this.GetTypeOfExpression(n.Right, function);

                    switch (n.Op.Operator) {
                        case Operator.Addition:
                        case Operator.Subtraction:
                            if (lt != WellKnownSymbol.Word && !(lt.Name == "ptr")) throw new WrongTypeException(n.Position, lt.Name);
                            if (rt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, rt.Name);

                            return lt;

                        case Operator.Multiplication:
                        case Operator.Division:
                        case Operator.Remainder:
                        case Operator.Exponentiation:
                        case Operator.ShiftLeft:
                        case Operator.ShiftRight:
                        case Operator.RotateLeft:
                        case Operator.RotateRight:
                            if (lt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, lt.Name);
                            if (rt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, rt.Name);

                            return lt;

                        case Operator.And:
                        case Operator.Or:
                        case Operator.Xor:
                        case Operator.NotAnd:
                        case Operator.NotOr:
                        case Operator.NotXor:
                            if (lt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, lt.Name);
                            if (rt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, rt.Name);

                            return lt;

                        case Operator.Equals:
                        case Operator.NotEquals:
                            if (lt != rt && !((lt.Name == "ptr" && rt == WellKnownSymbol.Word) || (rt.Name == "ptr" && lt == WellKnownSymbol.Word))) throw new WrongTypeException(n.Position, rt.Name);

                            return WellKnownSymbol.Bool;

                        case Operator.LessThan:
                        case Operator.LessThanOrEqual:
                        case Operator.GreaterThan:
                        case Operator.GreaterThanOrEqual:
                            if (lt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, lt.Name);
                            if (rt != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, rt.Name);

                            return WellKnownSymbol.Bool;
                    }

                    break;
            }

            throw new WrongTypeException(node.Position, "invalid operands");
        }
    }

    public abstract class Symbol {
        public string Name { get; }

        protected Symbol(string name) => this.Name = name;

        public override string ToString() => $"{this.Name}({this.GetType().Name})";
    }

    public sealed class FunctionSymbol : Symbol {
        private List<ArgumentSymbol> arguments;
        private List<LocalVariableSymbol> localVariables;

        public TypeSymbol Type { get; }
        public IReadOnlyCollection<ArgumentSymbol> Arguments => this.arguments;
        public IReadOnlyCollection<LocalVariableSymbol> LocalVariables => this.localVariables;

        public FunctionSymbol(string name, TypeSymbol type, IReadOnlyCollection<ArgumentSymbol> arguments, IReadOnlyCollection<LocalVariableSymbol> variables) : base(name) => (this.Type, this.arguments, this.localVariables) = (type, arguments.ToList(), variables.ToList());

        public void AddLocalVariable(LocalVariableSymbol variable) => this.localVariables.Add(variable);
    }

    public sealed class ArgumentSymbol : Symbol {
        public TypeSymbol Type { get; }

        public ArgumentSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class LocalVariableSymbol : Symbol {
        public TypeSymbol Type { get; }

        public LocalVariableSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class RegisterSymbol : Symbol {
        public Register Register { get; }

        public RegisterSymbol(string name, Register register) : base(name) => this.Register = register;
    }

    public sealed class GlobalVariableSymbol : Symbol {
        public TypeSymbol Type { get; }

        public GlobalVariableSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class ConstVariableSymbol : Symbol {
        public TypeSymbol Type { get; }
        public ulong Value { get; }

        public ConstVariableSymbol(string name, TypeSymbol type, ulong value) : base(name) => (this.Type, this.Value) = (type, value);
    }

    public sealed class TypeSymbol : Symbol {
        public IReadOnlyCollection<TypeSymbol> GenericArguments { get; }

        public string FullName => this.Name + (this.GenericArguments.Any() ? "[" + string.Join(", ", this.GenericArguments.Select(g => g.Name)) + "]" : string.Empty);

        public TypeSymbol(string name) : base(name) => this.GenericArguments = new List<TypeSymbol>();
        public TypeSymbol(string name, params TypeSymbol[] genericArguments) : base(name) => this.GenericArguments = genericArguments;

        public static bool operator ==(TypeSymbol lhs, TypeSymbol rhs) => lhs.FullName == rhs.FullName;
        public static bool operator !=(TypeSymbol lhs, TypeSymbol rhs) => !(lhs == rhs);
        public override bool Equals(object obj) => obj is TypeSymbol t && t == this;
        public override int GetHashCode() => this.FullName.GetHashCode();
    }
}

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class IrGenerator {
        private IrGenerator() { }

        public static Compiliation Generate(ProgramDeclarationNode ast) {
            var symbolTable = new SymbolTable(ast);
            var functions = ast.FunctionDeclarations.Select(i => FunctionDeclarationVisitor.Visit(symbolTable, i)).ToList();

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

        private LocalVariableLValue CreateTemporaryLocalVariable(TypeSymbol type) => new LocalVariableLValue(this.symbolTable.CreateTemporaryLocalVariable(this.functionSymbol, type));
        private LocalVariableLValue CreateTemporaryLocalVariable(TypeIdentifierNode node) => this.CreateTemporaryLocalVariable(this.symbolTable.FindType(node));

        public static Function Visit(SymbolTable symbolTable, FunctionDeclarationNode node) {
            var func = symbolTable.TryFindFunction(node.Identifier, out var f) ? f : throw new IdentifierNotFoundException(node.Position, node.Identifier);
            var visitor = new FunctionDeclarationVisitor(symbolTable, func);

            visitor.Visit(node.StatementBlock);

            visitor.block.PushTerminator(new ReturnTerminator(visitor.CreateTemporaryLocalVariable(node.Type)));

            return new Function(func, visitor.block.Entry);
        }

        private void Visit(StatementBlockNode node) {
            foreach (var b in node.Statements)
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
                case LocalVariableDeclarationWithInitializerNode n:
                    var lhsType = this.symbolTable.FindType(n.Type);
                    var rhsType = this.symbolTable.GetTypeOfExpression(n.Initializer, this.functionSymbol);

                    if (!((lhsType.Name == "ptr" && rhsType == WellKnownSymbol.Word) || (rhsType.Name == "ptr" && lhsType == WellKnownSymbol.Word)))
                        this.symbolTable.CheckTypeOfExpression(n.Type, n.Initializer, this.functionSymbol);

                    this.Visit(new AssignmentStatementNode(n.Position, new IdentifierNode(n.Token), n.Initializer));

                    break;
            }
        }

        private void Visit(ReturnStatementNode node) {
            this.block.PushTerminator(new ReturnTerminator(this.ExtractRValue(node.Expression)));
        }

        private void Visit(AssignmentStatementNode node) {
            var lhs = this.ExtractLValue(node.Target);
            var exp = node.Expression;

            if (node is CompoundAssignmentStatementNode cnode)
                exp = new BinaryExpressionNode(cnode.Position, cnode.Target, cnode.Op, cnode.Expression);

            var lhsType = this.symbolTable.GetTypeOfExpression(node.Target, this.functionSymbol);
            var rhsType = this.symbolTable.GetTypeOfExpression(exp, this.functionSymbol);

            if (!((lhsType.Name == "ptr" && rhsType == WellKnownSymbol.Word) || (rhsType.Name == "ptr" && lhsType == WellKnownSymbol.Word)))
                this.symbolTable.CheckTypeOfExpression(node.Target, exp, this.functionSymbol);

            this.block.PushInstuction(new BasicBlockAssignmentInstruction(lhs, this.ExtractRValue(exp)));
        }

        private void Visit(IfStatementNode node) {
            this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Bool, node.Expression, this.functionSymbol);

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
            this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Bool, node.Expression, this.functionSymbol);

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

            if (def.ParameterCount != node.ArgumentList.Count) throw new TooFewArgumentsException(node.Position, def.Mnemonic);

            RValue a = null, b = null, c = null;

            if (def.ParameterCount > 0) { node.ArgumentList.Extract(0, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.Extract(1, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.Extract(2, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }

            this.block.PushInstuction(new BasicBlockIntrinsicInstruction(def, a, b, c));
        }

        private void Visit(ExpressionStatementNode node) {
            switch (node) {
                default: throw new UnexpectedException(node.Position, "expression node");
                case FunctionCallIdentifierNode n: this.Visit(n); break;
            }
        }

        private LValue Visit(FunctionCallIdentifierNode node) {
            var args = node.ArgumentList.Select(a => this.ExtractRValue(a)).ToList();
            var func = this.symbolTable.TryFindFunction(node.Identifier, out var f) ? f : throw new IdentifierNotFoundException(node.Position, node.Identifier);
            var result = this.CreateTemporaryLocalVariable(func.Type);

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
            var type = this.symbolTable.GetTypeOfExpression(node, this.functionSymbol);

            switch (node) {
                default: throw new ExpectedException(node.Position, "lvalue");
                case IdentifierExpressionNode n: return this.ExtractLValue(n);
                case UnaryExpressionNode n when n.Op.Operator == Operator.Dereference: return new PointerLValue(this.ExtractRValue(n.Expression));
            }
        }

        private RValue ExtractRValue(ExpressionStatementNode node) {
            var type = this.symbolTable.GetTypeOfExpression(node, this.functionSymbol);

            switch (node) {
                case IntegerLiteralNode n: return new IntegerRValue(n.Literal);
                case BoolLiteralNode n: return new IntegerRValue(n.Literal ? ulong.MaxValue : 0);
                case FunctionCallIdentifierNode n: return this.Visit(n);
                case IdentifierExpressionNode n: return this.ExtractRValue(n);
                case UnaryExpressionNode n:
                    switch (n.Op.Operator) {
                        case Operator.UnaryMinus: return this.ExtractRValue(new BinaryExpressionNode(n.Position, n.Expression, OperatorNode.FromOperator(Operator.Multiplication), new IntegerLiteralNode(n.Position, ulong.MaxValue)));
                        case Operator.Not: return this.ExtractRValue(new BinaryExpressionNode(n.Position, n.Expression, OperatorNode.FromOperator(Operator.Xor), new IntegerLiteralNode(n.Position, ulong.MaxValue)));
                        case Operator.AddressOf: return new AddressOfRValue(this.ExtractLValue(n.Expression));
                        case Operator.Dereference: return new PointerLValue(this.ExtractRValue(n.Expression));
                    }

                    break;

                case BinaryExpressionNode n:
                    var target = this.CreateTemporaryLocalVariable(type);

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
