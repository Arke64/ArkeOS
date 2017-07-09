﻿using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using ArkeOS.Utilities.Extensions;

namespace ArkeOS.Tools.KohlCompiler {
    public sealed class Parser {
        private readonly Lexer lexer;

        private Parser(Lexer lexer) => this.lexer = lexer;

        public static ProgramDeclarationNode Parse(CompilationOptions options) => new Parser(new Lexer(options)).Parse();

        private ProgramDeclarationNode Parse() {
            var prog = new ProgramDeclarationNode();

            while (this.lexer.TryPeek(out var tok)) {
                switch (tok.Type) {
                    case TokenType.FuncKeyword: prog.FunctionDeclarations.Add(this.ReadFunctionDeclaration()); break;
                    case TokenType.VarKeyword: prog.VariableDeclarations.Add(this.ReadGlobalVariableDeclaration()); break;
                    case TokenType.ConstKeyword: prog.ConstDeclarations.Add(this.ReadConstDeclaration()); break;
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
            var list = new ArgumentListDeclarationNode();

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
                var node = new TypeIdentifierNode(ident, this.ReadTypeIdentifier());

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

        private GlobalVariableDeclarationNode ReadGlobalVariableDeclaration() {
            this.lexer.Read(TokenType.VarKeyword);
            var ident = this.lexer.Read(TokenType.Identifier);
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();
            this.lexer.Read(TokenType.Semicolon);

            return new GlobalVariableDeclarationNode(ident, type);
        }

        private LocalVariableDeclarationNode ReadLocalVariableDeclaration() {
            this.lexer.Read(TokenType.VarKeyword);
            var ident = this.lexer.Read(TokenType.Identifier);
            this.lexer.Read(TokenType.Colon);
            var type = this.ReadTypeIdentifier();
            var res = this.lexer.TryRead(TokenType.Equal) ? new LocalVariableDeclarationWithInitializerNode(ident, type, this.ReadExpression()) : new LocalVariableDeclarationNode(ident, type);
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
            var block = new StatementBlockNode();

            if (this.lexer.TryRead(TokenType.OpenCurlyBrace)) {
                while (!this.lexer.TryRead(TokenType.CloseCurlyBrace)) {
                    if (this.lexer.TryPeek(TokenType.VarKeyword)) {
                        var res = this.ReadLocalVariableDeclaration();

                        if (res is LocalVariableDeclarationWithInitializerNode)
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
                res = new EmptyStatementNode();

                this.lexer.Read(TokenType.Semicolon);
            }
            else {
                var lhs = this.ReadExpression();

                if (this.lexer.TryPeek(TokenClass.Assignment)) {
                    var op = this.lexer.Read(TokenClass.Assignment);
                    var rhs = this.ReadExpression();

                    res = op.Type == TokenType.Equal ? new AssignmentStatementNode(lhs, rhs) : new CompoundAssignmentStatementNode(lhs, OperatorNode.FromCompoundToken(op) ?? throw this.GetUnexpectedException(op), rhs);
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
            var list = new ArgumentListNode();

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
                case TokenType.BrkKeyword: return new BrkStatementNode();
                case TokenType.CasKeyword: return new CasStatementNode(this.ReadArgumentList());
                case TokenType.CpyKeyword: return new CpyStatementNode(this.ReadArgumentList());
                case TokenType.DbgKeyword: return new DbgStatementNode(this.ReadArgumentList());
                case TokenType.EintKeyword: return new EintStatementNode();
                case TokenType.HltKeyword: return new HltStatementNode();
                case TokenType.IntdKeyword: return new IntdStatementNode();
                case TokenType.InteKeyword: return new InteStatementNode();
                case TokenType.IntKeyword: return new IntStatementNode(this.ReadArgumentList());
                case TokenType.NopKeyword: return new NopStatementNode();
                case TokenType.XchgKeyword: return new XchgStatementNode(this.ReadArgumentList());
                default: throw this.GetUnexpectedException(tok);
            }
        }

        private IfStatementNode ReadIfStatement() {
            this.lexer.Read(TokenType.IfKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return !this.lexer.TryRead(TokenType.ElseKeyword) ? new IfStatementNode(exp, block) : new IfElseStatementNode(exp, block, this.ReadStatementBlock());
        }

        private WhileStatementNode ReadWhileStatement() {
            this.lexer.Read(TokenType.WhileKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return new WhileStatementNode(exp, block);
        }

        private ReturnStatementNode ReadReturnStatement() {
            this.lexer.Read(TokenType.ReturnKeyword);

            return new ReturnStatementNode(this.ReadExpression());
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
