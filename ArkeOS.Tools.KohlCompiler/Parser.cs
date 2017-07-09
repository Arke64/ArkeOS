﻿using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler {
    public sealed class Parser {
        private readonly Lexer lexer;

        private Parser(Lexer lexer) => this.lexer = lexer;

        public static ProgramNode Parse(CompilationOptions options) => new Parser(new Lexer(options)).Parse();

        private ProgramNode Parse() {
            var prog = new ProgramNode(this.lexer.CurrentPosition);

            while (this.lexer.TryPeek(out var tok)) {
                switch (tok.Type) {
                    case TokenType.FuncKeyword: prog.Add(this.ReadFunctionDeclaration()); break;
                    case TokenType.VarKeyword: prog.Add(this.ReadVariableDeclaration()); break;
                    case TokenType.ConstKeyword: prog.Add(this.ReadConstDeclaration()); break;
                    default: throw this.GetUnexpectedException(tok);
                }
            }

            return prog;
        }

        private FunctionDeclarationNode ReadFunctionDeclaration() {
            this.lexer.Read(TokenType.FuncKeyword);
            var ident = this.lexer.Read(TokenType.Identifier);
            var args = this.ReadArgumentListDeclaration();
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();
            var block = this.ReadStatementBlock();

            return new FunctionDeclarationNode(ident, type, args, block);
        }

        private ArgumentListDeclarationNode ReadArgumentListDeclaration() {
            var list = new ArgumentListDeclarationNode(this.lexer.CurrentPosition);

            this.lexer.Read(TokenType.OpenParenthesis);

            if (!this.lexer.TryRead(TokenType.CloseParenthesis)) {
                do {
                    list.Add(this.ReadArgumentDeclaration());
                } while (this.lexer.TryRead(TokenType.Comma));

                this.lexer.Read(TokenType.CloseParenthesis);
            }

            return list;
        }

        private TypeIdentifierNode ReadTypeIdentifier() {
            var ident = this.lexer.Read(TokenType.Identifier);

            if (this.lexer.TryRead(TokenType.OpenSquareBrace)) {
                var node = new TypeIdentifierNode(ident, new List<TypeIdentifierNode> { this.ReadTypeIdentifier() });

                this.lexer.Read(TokenType.CloseSquareBrace);

                return node;
            }
            else {
                return new TypeIdentifierNode(ident);
            }
        }

        private ArgumentDeclarationNode ReadArgumentDeclaration() {
            var ident = this.lexer.Read(TokenType.Identifier);
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();

            return new ArgumentDeclarationNode(ident, type);
        }

        private VariableDeclarationNode ReadVariableDeclaration() {
            this.lexer.Read(TokenType.VarKeyword);
            var ident = this.lexer.Read(TokenType.Identifier);
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();
            this.lexer.TryRead(TokenType.Equal);
            var res = new VariableDeclarationNode(ident, type, this.ReadExpression());
            this.lexer.Read(TokenType.Semicolon);

            return res;
        }

        private ConstDeclarationNode ReadConstDeclaration() {
            this.lexer.Read(TokenType.ConstKeyword);
            var ident = this.lexer.Read(TokenType.Identifier);
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();
            this.lexer.Read(TokenType.Equal);
            var tok = this.lexer.Read(TokenType.IntegerLiteral);
            this.lexer.Read(TokenType.Semicolon);

            return new ConstDeclarationNode(ident, type, new IntegerLiteralNode(tok));
        }

        private StatementBlockNode ReadStatementBlock() {
            var block = new StatementBlockNode(this.lexer.CurrentPosition);

            if (this.lexer.TryRead(TokenType.OpenCurlyBrace)) {
                while (!this.lexer.TryRead(TokenType.CloseCurlyBrace)) {
                    if (this.lexer.TryPeek(TokenType.VarKeyword)) {
                        var res = this.ReadVariableDeclaration();

                        if (res is VariableDeclarationNode)
                            block.Statements.Add(res);

                        block.VariableDeclarations.Add(res);
                    }
                    else {
                        block.Statements.Add(this.ReadStatement());
                    }
                }
            }
            else {
                block.Statements.Add(this.ReadStatement());
            }

            return block;
        }

        private StatementNode ReadStatement() {
            var tok = this.lexer.Peek();
            var res = default(StatementNode);

            if (tok.Class == TokenClass.IntrinsicKeyword) {
                res = this.ReadIntrinsicStatement();

                this.lexer.Read(TokenType.Semicolon);
            }
            else if (tok.Type == TokenType.IfKeyword) {
                res = this.ReadIfStatement();
            }
            else if (tok.Type == TokenType.WhileKeyword) {
                res = this.ReadWhileStatement();
            }
            else if (tok.Type == TokenType.ReturnKeyword) {
                res = this.ReadReturnStatement();

                this.lexer.Read(TokenType.Semicolon);
            }
            else if (tok.Type == TokenType.Semicolon) {
                res = new EmptyStatementNode(tok.Position);

                this.lexer.Read(TokenType.Semicolon);
            }
            else {
                var lhs = this.ReadExpression();

                if (this.lexer.TryPeek(TokenClass.Assignment)) {
                    var op = this.lexer.Read(TokenClass.Assignment);
                    var rhs = this.ReadExpression();

                    res = op.Type == TokenType.Equal ? new AssignmentStatementNode(lhs.Position, lhs, rhs) : new CompoundAssignmentStatementNode(lhs.Position, lhs, OperatorNode.FromCompoundToken(op) ?? throw this.GetUnexpectedException(op), rhs);
                }
                else if (lhs is FunctionCallIdentifierNode) {
                    res = lhs;
                }
                else {
                    throw this.GetExpectedException(lhs.Position, "statement");
                }

                this.lexer.Read(TokenType.Semicolon);
            }

            return res;
        }

        private IdentifierExpressionNode ReadIdentifier() {
            var tok = this.lexer.Read(TokenType.Identifier);

            if (this.lexer.TryPeek(TokenType.OpenParenthesis)) {
                return new FunctionCallIdentifierNode(tok, this.ReadArgumentList());
            }
            else {
                return tok.Value.IsValidEnum<Register>() ? new RegisterIdentifierNode(tok) : (IdentifierExpressionNode)new IdentifierNode(tok);
            }
        }

        private LiteralExpressionNode ReadLiteral() {
            var tok = this.lexer.Read(TokenClass.Literal);

            switch (tok.Type) {
                case TokenType.IntegerLiteral: return new IntegerLiteralNode(tok);
                case TokenType.BoolLiteral: return new BoolLiteralNode(tok);
                default: throw this.GetUnexpectedException(tok);
            }
        }

        private ArgumentListNode ReadArgumentList() {
            var list = new ArgumentListNode(this.lexer.CurrentPosition);

            this.lexer.Read(TokenType.OpenParenthesis);

            if (!this.lexer.TryRead(TokenType.CloseParenthesis)) {
                do {
                    list.Add(this.ReadExpression());
                } while (this.lexer.TryRead(TokenType.Comma));

                this.lexer.Read(TokenType.CloseParenthesis);
            }

            return list;
        }

        private IntrinsicStatementNode ReadIntrinsicStatement() {
            var tok = this.lexer.Read();

            switch (tok.Type) {
                case TokenType.BrkKeyword: return new BrkStatementNode(tok.Position);
                case TokenType.CasKeyword: return new CasStatementNode(tok.Position, this.ReadArgumentList());
                case TokenType.CpyKeyword: return new CpyStatementNode(tok.Position, this.ReadArgumentList());
                case TokenType.DbgKeyword: return new DbgStatementNode(tok.Position, this.ReadArgumentList());
                case TokenType.EintKeyword: return new EintStatementNode(tok.Position);
                case TokenType.HltKeyword: return new HltStatementNode(tok.Position);
                case TokenType.IntdKeyword: return new IntdStatementNode(tok.Position);
                case TokenType.InteKeyword: return new InteStatementNode(tok.Position);
                case TokenType.IntKeyword: return new IntStatementNode(tok.Position, this.ReadArgumentList());
                case TokenType.NopKeyword: return new NopStatementNode(tok.Position);
                case TokenType.XchgKeyword: return new XchgStatementNode(tok.Position, this.ReadArgumentList());
                default: throw this.GetUnexpectedException(tok);
            }
        }

        private IfStatementNode ReadIfStatement() {
            var tok = this.lexer.Read(TokenType.IfKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return !this.lexer.TryRead(TokenType.ElseKeyword) ? new IfStatementNode(tok.Position, exp, block) : new IfElseStatementNode(tok.Position, exp, block, this.ReadStatementBlock());
        }

        private WhileStatementNode ReadWhileStatement() {
            var tok = this.lexer.Read(TokenType.WhileKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return new WhileStatementNode(tok.Position, exp, block);
        }

        private ReturnStatementNode ReadReturnStatement() {
            var tok = this.lexer.Read(TokenType.ReturnKeyword);

            return new ReturnStatementNode(tok.Position, this.ReadExpression());
        }

        private ExpressionStatementNode ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;
            var seenOpenParens = 0;

            while (true) {
                var tok = this.lexer.Peek();

                if (tok.Type == TokenType.OpenParenthesis) seenOpenParens++;

                if (start && tok.Class == TokenClass.Identifier) {
                    stack.Push(this.ReadIdentifier());
                    start = false;
                }
                else if (start && tok.Class == TokenClass.Literal) {
                    stack.Push(this.ReadLiteral());
                    start = false;
                }
                else if (!start && tok.Type == TokenType.CloseParenthesis) {
                    if (--seenOpenParens < 0)
                        break;

                    stack.Push(this.ReadOperator(OperatorClass.Binary));
                    start = false;
                }
                else if (tok.Class == TokenClass.Operator) {
                    stack.Push(this.ReadOperator(start && tok.Type != TokenType.OpenParenthesis ? OperatorClass.Unary : OperatorClass.Binary));
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode() ?? throw this.GetExpectedException(default(PositionInfo), "expression");
        }

        private OperatorNode ReadOperator(OperatorClass cls) {
            var token = this.lexer.Read(TokenClass.Operator);

            return OperatorNode.FromToken(token, cls) ?? throw this.GetUnexpectedException(token);
        }

        private UnexpectedException GetUnexpectedException(Token t) => new UnexpectedException(t.Position, t.Type);
        private ExpectedException GetExpectedException(PositionInfo position, string t) => new ExpectedException(position, t);
    }
}
