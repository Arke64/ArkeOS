using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public sealed class SymbolTable {
        private static IReadOnlyCollection<TypeSymbol> WellKnownTypes { get; } = new List<TypeSymbol> { WellKnownSymbol.Word, WellKnownSymbol.Bool };

        private ulong nextTemporarySymbolId = 0;

        public IReadOnlyCollection<ConstVariableSymbol> ConstVariables { get; }
        public IReadOnlyCollection<GlobalVariableSymbol> GlobalVariables { get; }
        public IReadOnlyCollection<RegisterSymbol> Registers { get; }
        public IReadOnlyCollection<FunctionSymbol> Functions { get; }
        public IReadOnlyCollection<StructSymbol> Structs { get; }

        public SymbolTable(ProgramNode ast) {
            this.Registers = EnumExtensions.ToList<Register>().Select(i => new RegisterSymbol(i.ToString(), i)).ToList();

            var structs = ast.OfType<StructDeclarationNode>().Select(s => (s, new StructSymbol(s.Identifier))).ToList();
            this.Structs = structs.Select(i => i.Item2).ToList();

            foreach (var s in structs) {
                var offset = 0UL;

                for (var i = 0; i < s.Item1.Members.Count; i++) {
                    var m = s.Item1.Members[i];

                    s.Item2.AddMember(new StructMemberSymbol(m.Identifier, this.FindType(m.Type), offset));

                    offset += s.Item2.Members[i].Type.Size;
                }
            }

            this.Functions = ast.OfType<FunctionDeclarationNode>().Select(i => this.Visit(i)).ToList();
            this.ConstVariables = ast.OfType<ConstDeclarationNode>().Select(i => new ConstVariableSymbol(i.Identifier, this.FindType(i.Type), ((IntegerLiteralNode)i.Value).Literal)).ToList();
            this.GlobalVariables = ast.OfType<VariableDeclarationNode>().Select(i => new GlobalVariableSymbol(i.Identifier, this.FindType(i.Type))).ToList();
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
        public StructMemberSymbol FindStructMember(PositionInfo position, StructSymbol strct, string name) => this.TryFindStructMember(strct, name, out var r) ? r : throw new IdentifierNotFoundException(position, name);

        public bool TryFindType(string name, out TypeSymbol result) => this.TryFind(SymbolTable.WellKnownTypes, name, out result) || this.TryFind(this.Structs, name, out result);
        public bool TryFindConstVariable(string name, out ConstVariableSymbol result) => this.TryFind(this.ConstVariables, name, out result);
        public bool TryFindGlobalVariable(string name, out GlobalVariableSymbol result) => this.TryFind(this.GlobalVariables, name, out result);
        public bool TryFindRegister(string name, out RegisterSymbol result) => this.TryFind(this.Registers, name, out result);
        public bool TryFindFunction(string name, out FunctionSymbol result) => this.TryFind(this.Functions, name, out result);
        public bool TryFindArgument(FunctionSymbol function, string name, out ArgumentSymbol result) => this.TryFind(function.Arguments, name, out result);
        public bool TryFindLocalVariable(FunctionSymbol function, string name, out LocalVariableSymbol result) => this.TryFind(function.LocalVariables, name, out result);
        public bool TryFindStructMember(StructSymbol strct, string name, out StructMemberSymbol result) => this.TryFind(strct.Members, name, out result);

        private TypeSymbol GetTypeSymbol(TypeIdentifierNode node) {
            var count = 0;

            while (node.GenericArguments.Any()) {
                count++;
                node = node.GenericArguments.Single();
            }

            var type = this.TryFindType(node.Identifier, out var r) ? r : throw new IdentifierNotFoundException(node.Position, node.Identifier);

            while (count-- > 0)
                type = new TypeSymbol("ptr", 1, new List<TypeSymbol> { type });

            return type;
        }

        public void CheckAssignable(TypeIdentifierNode target, ExpressionStatementNode exp, FunctionSymbol functionSymbol) {
            var lhsType = this.FindType(target);
            var rhsType = this.GetTypeOfExpression(exp, functionSymbol);

            if (!((lhsType.BaseName == "ptr" && rhsType == WellKnownSymbol.Word) || (rhsType.BaseName == "ptr" && lhsType == WellKnownSymbol.Word)))
                this.CheckTypeOfExpression(target, exp, functionSymbol);
        }

        public void CheckAssignable(ExpressionStatementNode target, ExpressionStatementNode exp, FunctionSymbol functionSymbol) {
            var lhsType = this.GetTypeOfExpression(target, functionSymbol);
            var rhsType = this.GetTypeOfExpression(exp, functionSymbol);

            if (!((lhsType.BaseName == "ptr" && rhsType == WellKnownSymbol.Word) || (rhsType.BaseName == "ptr" && lhsType == WellKnownSymbol.Word)))
                this.CheckTypeOfExpression(target, exp, functionSymbol);
        }

        public void CheckAssignable(TypeSymbol target, ExpressionStatementNode exp, FunctionSymbol functionSymbol) {
            var lhsType = target;
            var rhsType = this.GetTypeOfExpression(exp, functionSymbol);

            if (!((lhsType.BaseName == "ptr" && rhsType == WellKnownSymbol.Word) || (rhsType.BaseName == "ptr" && lhsType == WellKnownSymbol.Word)))
                this.CheckTypeOfExpression(target, exp, functionSymbol);
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

        public TypeSymbol GetTypeOfExpression(ExpressionStatementNode node, FunctionSymbol function) => this.GetTypeOfExpression(node, function, null);

        public TypeSymbol GetTypeOfExpression(ExpressionStatementNode node, FunctionSymbol function, StructSymbol parent) {
            switch (node) {
                case IntegerLiteralNode n: return WellKnownSymbol.Word;
                case BoolLiteralNode n: return WellKnownSymbol.Bool;
                case FunctionCallIdentifierNode n: return this.TryFindFunction(n.Identifier, out var f) ? f.Type : throw new IdentifierNotFoundException(n.Position, n.Identifier);

                case MemberDereferenceIdentifierNode n: {
                        var i = this.VisitIdentifier(n, function, parent);

                        if (i.BaseName != "ptr") throw new WrongTypeException(n.Position, "not a ptr: " + n.Identifier);

                        var s = i.TypeArguments.Single() is StructSymbol ss ? ss : throw new WrongTypeException(n.Position, n.Identifier);

                        this.FindStructMember(n.Position, s, n.Member.Identifier);

                        return this.GetTypeOfExpression(n.Member, function, s);
                    }

                case MemberAccessIdentifierNode n: {
                        var s = this.VisitIdentifier(n, function, parent) is StructSymbol ss ? ss : throw new WrongTypeException(n.Position, n.Identifier);

                        this.FindStructMember(n.Position, s, n.Member.Identifier);

                        return this.GetTypeOfExpression(n.Member, function, s);
                    }

                case IdentifierExpressionNode n: return this.VisitIdentifier(n, function, parent);

                case UnaryExpressionNode n:
                    var t = this.GetTypeOfExpression(n.Expression, function);

                    switch (n.Op.Operator) {
                        case Operator.UnaryMinus:
                            if (t != WellKnownSymbol.Word) throw new WrongTypeException(n.Position, t.Name);

                            return t;

                        case Operator.Not:
                            return t;

                        case Operator.AddressOf:
                            return new TypeSymbol("ptr", 1, new List<TypeSymbol> { t });

                        case Operator.Dereference:
                            return t.TypeArguments.Single();
                    }

                    break;

                case BinaryExpressionNode n:
                    var lt = this.GetTypeOfExpression(n.Left, function);
                    var rt = this.GetTypeOfExpression(n.Right, function);

                    switch (n.Op.Operator) {
                        case Operator.Addition:
                        case Operator.Subtraction:
                            if (lt != WellKnownSymbol.Word && !(lt.BaseName == "ptr")) throw new WrongTypeException(n.Position, lt.Name);
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
                            if (lt != WellKnownSymbol.Word && lt != WellKnownSymbol.Bool) throw new WrongTypeException(n.Position, lt.Name);
                            if (rt != WellKnownSymbol.Word && rt != WellKnownSymbol.Bool) throw new WrongTypeException(n.Position, rt.Name);
                            if (lt != rt) throw new WrongTypeException(n.Position, rt.Name);

                            return lt;

                        case Operator.Equals:
                        case Operator.NotEquals:
                            if (lt != rt && !((lt.BaseName == "ptr" && rt == WellKnownSymbol.Word) || (rt.BaseName == "ptr" && lt == WellKnownSymbol.Word))) throw new WrongTypeException(n.Position, rt.Name);

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

        private TypeSymbol VisitIdentifier(IdentifierExpressionNode n, FunctionSymbol function, StructSymbol strct) {
            if (strct != null && this.TryFindStructMember(strct, n.Identifier, out var ss)) return ss.Type;
            if (this.TryFindRegister(n.Identifier, out var rs)) return WellKnownSymbol.Word;
            if (function != null && this.TryFindArgument(function, n.Identifier, out var ps)) return ps.Type;
            if (function != null && this.TryFindLocalVariable(function, n.Identifier, out var ls)) return ls.Type;
            if (this.TryFindGlobalVariable(n.Identifier, out var gs)) return gs.Type;
            if (this.TryFindConstVariable(n.Identifier, out var cs)) return cs.Type;
            if (this.TryFindFunction(n.Identifier, out var fs)) return fs.Type;
            if (this.TryFindType(n.Identifier, out var ts)) return ts;

            throw new WrongTypeException(n.Position, n.Identifier);
        }
    }
}
