using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;

namespace ArkeOS.Tools.KohlCompiler {
    public enum Operator {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Remainder,
        Exponentiation,
        UnaryPlus,
        UnaryMinus,
        OpenParenthesis,
        CloseParenthesis,
    }

    public class Parser {
        private readonly TokenStream tokens;

        public Parser(TokenStream tokens) => this.tokens = tokens;

        public ProgramNode Parse() {
            var res = new ProgramNode();

            while (!this.tokens.AtEnd)
                res.Add(this.ReadAssignment());

            return res;
        }

        private void Read(TokenType t) {
            if (!this.tokens.Read(t))
                throw this.GetExpectedTokenExceptionAtCurrent(t);
        }

        private AssignmentNode ReadAssignment() {
            var ident = this.ReadIdentifier();
            this.Read(TokenType.EqualsSign);
            var exp = this.ReadExpression();
            this.Read(TokenType.Semicolon);

            return new AssignmentNode(ident, exp);
        }

        private Node ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;

            while (this.tokens.Peek(out var token)) {
                if (start && token.Type == TokenType.Number) {
                    stack.Push(this.ReadNumber());
                    start = false;
                }
                else if (start && token.Type == TokenType.Identifier) {
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

        private NumberNode ReadNumber() => this.tokens.Read(TokenType.Number, out var token) ? new NumberNode(token.Value) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Number);
        private IdentifierNode ReadIdentifier() => this.tokens.Read(TokenType.Identifier, out var token) ? new IdentifierNode(token.Value) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Identifier);

        private Operator ReadOperator(bool unary) {
            if (!this.tokens.Read(this.IsOperator, out var token))
                throw this.GetExpectedTokenExceptionAtCurrent("operator");

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

            throw this.GetUnexpectedTokenExceptionAtCurrent(token.Type);
        }

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

        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(TokenType t) {
            this.tokens.GetCurrentPositionInfo(out var file, out var line, out var column);

            return new UnexpectedTokenException(file, line, column, t);
        }

        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(string t) {
            this.tokens.GetCurrentPositionInfo(out var file, out var line, out var column);

            return new UnexpectedTokenException(file, line, column, t);
        }

        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(TokenType t) {
            this.tokens.GetCurrentPositionInfo(out var file, out var line, out var column);

            return new ExpectedTokenException(file, line, column, t);
        }

        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(string t) {
            this.tokens.GetCurrentPositionInfo(out var file, out var line, out var column);

            return new ExpectedTokenException(file, line, column, t);
        }
    }
}