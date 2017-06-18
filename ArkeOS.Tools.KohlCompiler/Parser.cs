using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;

namespace ArkeOS.Tools.KohlCompiler {
    public class Parser {
        private readonly Lexer lexer;

        public Parser(Lexer lexer) => this.lexer = lexer;

        public ProgramNode Parse() {
            var res = new ProgramNode(this.ReadStatementBlock());

            if (this.lexer.Peek(out var t))
                throw this.GetUnexpectedTokenExceptionAtCurrent(t.Type);

            return res;
        }

        private void Read(TokenType t) {
            if (!this.lexer.Read(t))
                throw this.GetExpectedTokenExceptionAtCurrent(t);
        }

        private StatementBlockNode ReadStatementBlock() {
            var block = new StatementBlockNode();

            if (this.lexer.Peek(out var t)) {
                if (t.Type == TokenType.OpenCurlyBrace) {
                    this.Read(TokenType.OpenCurlyBrace);

                    while (this.lexer.Peek(out t) && t.Type != TokenType.CloseCurlyBrace)
                        block.Add(this.ReadStatement());

                    this.Read(TokenType.CloseCurlyBrace);
                }
                else {
                    block.Add(this.ReadStatement());
                }
            }

            return block;
        }

        private StatementNode ReadStatement() {
            var res = this.ReadAssignment();

            this.Read(TokenType.Semicolon);

            return res;
        }

        private AssignmentNode ReadAssignment() {
            var ident = this.ReadIdentifier();
            this.Read(TokenType.EqualsSign);
            var exp = this.ReadExpression();

            return new AssignmentNode(ident, exp);
        }

        private ExpressionNode ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;

            while (this.lexer.Peek(out var token)) {
                if (start && token.Type == TokenType.Number) {
                    stack.Push(this.ReadNumber());
                    start = false;
                }
                else if (start && token.Type == TokenType.Identifier) {
                    stack.Push(this.ReadIdentifier());
                    start = false;
                }
                else if (!start && token.Type == TokenType.CloseParenthesis) {
                    stack.Push(this.ReadOperator(false), this.lexer.CurrentPosition);
                    start = false;
                }
                else if (token.IsOperator()) {
                    stack.Push(this.ReadOperator(start && token.Type != TokenType.OpenParenthesis), this.lexer.CurrentPosition);
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode();
        }

        private NumberNode ReadNumber() => this.lexer.Read(TokenType.Number, out var token) ? new NumberNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Number);
        private IdentifierNode ReadIdentifier() => this.lexer.Read(TokenType.Identifier, out var token) ? new IdentifierNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Identifier);

        private OperatorNode ReadOperator(bool unary) {
            if (!this.lexer.Read(t => t.IsOperator(), out var token))
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

            return new OperatorNode(res);
        }

        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(TokenType t) => new UnexpectedTokenException(this.lexer.CurrentPosition, t);
        private UnexpectedTokenException GetUnexpectedTokenExceptionAtCurrent(string t) => new UnexpectedTokenException(this.lexer.CurrentPosition, t);
        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(TokenType t) => new ExpectedTokenException(this.lexer.CurrentPosition, t);
        private ExpectedTokenException GetExpectedTokenExceptionAtCurrent(string t) => new ExpectedTokenException(this.lexer.CurrentPosition, t);
    }
}
