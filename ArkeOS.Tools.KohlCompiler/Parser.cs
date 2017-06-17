﻿using ArkeOS.Tools.KohlCompiler.Nodes;
using System;

namespace ArkeOS.Tools.KohlCompiler {
    public class Parser {
        private readonly TokenStream tokens;

        public Parser(TokenStream tokens) => this.tokens = tokens;

        public ProgramNode Parse() {
            var res = new ProgramNode();

            while (this.tokens.Peek(out _))
                res.Add(this.ReadAssignment());

            return res;
        }

        private AssignmentNode ReadAssignment() {
            var ident = this.ReadIdentifier();
            this.tokens.Read(TokenType.EqualsSign);
            var exp = this.ReadExpression();
            this.tokens.Read(TokenType.Semicolon);

            return new AssignmentNode(ident, exp);
        }

        private Node ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;

            while (this.tokens.Peek(out var token)) {
                if (start && (token.Type == TokenType.Number || token.Type == TokenType.Identifier)) {
                    if (token.Type == TokenType.Number)
                        stack.Push(this.ReadNumber());
                    else
                        stack.Push(this.ReadIdentifier());

                    start = false;
                }
                else if (!start && token.Type == TokenType.CloseParenthesis) {
                    stack.Push(this.ReadOperator(false));
                    start = false;
                }
                else if (this.IsOperator(token)) {
                    stack.Push(this.ReadOperator(start && token.Type != TokenType.OpenParenthesis));
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode();
        }

        private Operator ReadOperator(bool unary) => this.tokens.Read(this.IsOperator, out var token) ? this.GetOperator(token, unary) : throw new InvalidOperationException("Expected token");

        private NumberNode ReadNumber() => this.tokens.Read(TokenType.Number, out var token) ? new NumberNode(token.Value) : throw new InvalidOperationException("Expected token");
        private IdentifierNode ReadIdentifier() => this.tokens.Read(TokenType.Identifier, out var token) ? new IdentifierNode(token.Value) : throw new InvalidOperationException("Expected token");

        private bool IsOperator(Token token) {
            switch (token.Type) {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Percent:
                case TokenType.Caret:
                case TokenType.OpenParenthesis:
                case TokenType.CloseParenthesis:
                    return true;
            }

            return false;
        }

        private Operator GetOperator(Token token, bool unary) {
            if (!unary) {
                switch (token.Type) {
                    case TokenType.Plus: return Operator.Addition;
                    case TokenType.Minus: return Operator.Subtraction;
                    case TokenType.Asterisk: return Operator.Multiplication;
                    case TokenType.ForwardSlash: return Operator.Division;
                    case TokenType.Percent: return Operator.Remainder;
                    case TokenType.Caret: return Operator.Exponentiation;
                    case TokenType.OpenParenthesis: return Operator.OpenParenthesis;
                    case TokenType.CloseParenthesis: return Operator.CloseParenthesis;
                }
            }
            else {
                switch (token.Type) {
                    case TokenType.Plus: return Operator.UnaryPlus;
                    case TokenType.Minus: return Operator.UnaryMinus;
                }
            }

            throw new InvalidOperationException("Expected token");
        }
    }
}