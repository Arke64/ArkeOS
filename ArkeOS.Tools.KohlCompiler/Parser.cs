using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;

namespace ArkeOS.Tools.KohlCompiler {
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
                    stack.Push(this.ReadOperator(false, out var p), p);
                    start = false;
                }
                else if (this.IsOperator(token)) {
                    stack.Push(this.ReadOperator(start && token.Type != TokenType.OpenParenthesis, out var p), p);
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode();
        }

        private NumberNode ReadNumber() => this.tokens.Read(TokenType.Number, out var token) ? new NumberNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Number);
        private IdentifierNode ReadIdentifier() => this.tokens.Read(TokenType.Identifier, out var token) ? new IdentifierNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Identifier);
        private OperatorNode ReadOperator(bool unary) => this.ReadOperator(unary, out _);

        private OperatorNode ReadOperator(bool unary, out PositionInfo position) {
            if (!this.tokens.Read(this.IsOperator, out var token))
                throw this.GetExpectedTokenExceptionAtCurrent("operator");

            var res = default(Operator);

            if (!unary) {
                switch (token.Type) {
                    case TokenType.Plus: res = Operator.Addition; break;
                    case TokenType.Minus: res = Operator.Subtraction; break;
                    case TokenType.Asterisk: res = Operator.Multiplication; break;
                    case TokenType.ForwardSlash: res = Operator.Division; break;
                    case TokenType.Percent: res = Operator.Remainder; break;
                    case TokenType.Caret: res = Operator.Exponentiation; break;
                    case TokenType.OpenParenthesis: res = Operator.OpenParenthesis; break;
                    case TokenType.CloseParenthesis: res = Operator.CloseParenthesis; break;
                    default: throw this.GetUnexpectedTokenExceptionAtCurrent(token.Type);
                }
            }
            else {
                switch (token.Type) {
                    case TokenType.Plus: res = Operator.UnaryPlus; break;
                    case TokenType.Minus: res = Operator.UnaryMinus; break;
                    default: throw this.GetUnexpectedTokenExceptionAtCurrent(token.Type);
                }
            }

            position = this.tokens.CurrentPosition;

            return new OperatorNode(res);
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

        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(TokenType t) => new UnexpectedTokenException(this.tokens.CurrentPosition, t);
        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(string t) => new UnexpectedTokenException(this.tokens.CurrentPosition, t);
        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(TokenType t) => new ExpectedTokenException(this.tokens.CurrentPosition, t);
        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(string t) => new ExpectedTokenException(this.tokens.CurrentPosition, t);
    }
}