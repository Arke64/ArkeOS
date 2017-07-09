using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Analysis;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class IrGenerator {
        private readonly BasicBlockCreator block = new BasicBlockCreator();
        private readonly SymbolTable symbolTable;
        private readonly FunctionSymbol functionSymbol;

        private IrGenerator(SymbolTable symbolTable, FunctionSymbol functionSymbol) => (this.symbolTable, this.functionSymbol) = (symbolTable, functionSymbol);

        public static Compiliation Generate(ProgramNode ast) {
            var symbolTable = new SymbolTable(ast);
            var functions = new List<Function>();

            foreach (var node in ast.OfType<FunctionDeclarationNode>()) {
                var func = symbolTable.TryFindFunction(node.Identifier, out var f) ? f : throw new IdentifierNotFoundException(node.Position, node.Identifier);
                var visitor = new IrGenerator(symbolTable, func);

                visitor.Visit(node.StatementBlock);

                visitor.block.PushTerminator(new ReturnTerminator(visitor.CreateTemporaryLocalVariable(node.Type)));

                functions.Add(new Function(func, visitor.block.Entry));
            }

            return new Compiliation(functions, symbolTable.GlobalVariables);
        }

        private LocalVariableLValue CreateTemporaryLocalVariable(TypeSymbol type) => new LocalVariableLValue(this.symbolTable.CreateTemporaryLocalVariable(this.functionSymbol, type));
        private LocalVariableLValue CreateTemporaryLocalVariable(TypeIdentifierNode node) => this.CreateTemporaryLocalVariable(this.symbolTable.FindType(node));

        private void Visit(StatementBlockNode node) {
            foreach (var b in node)
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
                case VariableDeclarationNode n:
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

            if (def.ParameterCount != node.ArgumentList.Count) throw new WrongNumberOfArgumentsException(node.Position, def.Mnemonic);

            RValue a = null, b = null, c = null;

            if (def.ParameterCount > 0) { node.ArgumentList.TryGet(0, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); a = def.Parameter1Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 1) { node.ArgumentList.TryGet(1, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); b = def.Parameter2Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }
            if (def.ParameterCount > 2) { node.ArgumentList.TryGet(2, out var arg); this.symbolTable.CheckTypeOfExpression(WellKnownSymbol.Word, arg, this.functionSymbol); c = def.Parameter3Direction.HasFlag(ParameterDirection.Write) ? this.ExtractLValue(arg) : this.ExtractRValue(arg); }

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
                        case Operator.UnaryMinus: return this.ExtractRValue(new BinaryExpressionNode(n.Position, n.Expression, OperatorNode.FromOperator(n.Position, Operator.Multiplication), new IntegerLiteralNode(n.Position, ulong.MaxValue)));
                        case Operator.Not: return this.ExtractRValue(new BinaryExpressionNode(n.Position, n.Expression, OperatorNode.FromOperator(n.Position, Operator.Xor), new IntegerLiteralNode(n.Position, ulong.MaxValue)));
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
}
