using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
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

        public SymbolTable(ProgramNode ast) {
            this.ConstVariables = ast.OfType<ConstDeclarationNode>().Select(i => new ConstVariableSymbol(i.Identifier, this.FindType(i.Type), ((IntegerLiteralNode)i.Value).Literal)).ToList();
            this.GlobalVariables = ast.OfType<VariableDeclarationNode>().Select(i => new GlobalVariableSymbol(i.Identifier, this.FindType(i.Type))).ToList();
            this.Registers = EnumExtensions.ToList<Register>().Select(i => new RegisterSymbol(i.ToString(), i)).ToList();
            this.Functions = ast.OfType<FunctionDeclarationNode>().Select(i => this.Visit(i)).ToList();
        }

        private FunctionSymbol Visit(FunctionDeclarationNode node) {
            var variables = new List<LocalVariableSymbol>();

            void visitStatementBlock(StatementBlockNode n) => n.ForEach(s => visitStatement(s));

            void visitStatement(StatementNode n)
            {
                switch (n) {
                    case VariableDeclarationNode s: variables.Add(new LocalVariableSymbol(s.Identifier, this.FindType(s.Type))); break;
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
