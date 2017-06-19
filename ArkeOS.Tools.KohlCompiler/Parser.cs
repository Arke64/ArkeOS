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

        private void Peek(TokenType t) {
            if (!this.lexer.Read(t))
                throw this.GetExpectedTokenExceptionAtCurrent(t);
        }

        private bool TryRead(TokenType t) => this.lexer.Read(t);
        private bool TryPeek(TokenType t) => this.lexer.Peek(t);

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
            var res = default(StatementNode);

            if (this.lexer.Peek(out var t)) {
                switch (t.Type) {
                    case TokenType.BrkKeyword: res = this.ReadBrkStatement(); break;
                    case TokenType.CasKeyword: res = this.ReadCasStatement(); break;
                    case TokenType.CpyKeyword: res = this.ReadCpyStatement(); break;
                    case TokenType.DbgKeyword: res = this.ReadDbgStatement(); break;
                    case TokenType.EintKeyword: res = this.ReadEintStatement(); break;
                    case TokenType.HltKeyword: res = this.ReadHltStatement(); break;
                    case TokenType.IntdKeyword: res = this.ReadIntdStatement(); break;
                    case TokenType.InteKeyword: res = this.ReadInteStatement(); break;
                    case TokenType.IntKeyword: res = this.ReadIntStatement(); break;
                    case TokenType.NopKeyword: res = this.ReadNopStatement(); break;
                    case TokenType.XchgKeyword: res = this.ReadXchgStatement(); break;
                    case TokenType.IfKeyword: res = this.ReadIfStatement(); break;
                    case TokenType.Identifier: res = this.ReadAssignmentStatement(); break;
                }
            }

            return res;
        }

        private ArgumentListNode ReadArgumentList() {
            var list = new ArgumentListNode();

            this.Read(TokenType.OpenParenthesis);

            if (!this.TryPeek(TokenType.CloseParenthesis)) {
                do {
                    list.Add(this.ReadExpression());
                } while (this.TryRead(TokenType.Comma));
            }

            this.Read(TokenType.CloseParenthesis);

            return list;
        }

        private BrkStatementNode ReadBrkStatement() {
            this.Read(TokenType.BrkKeyword);
            this.Read(TokenType.Semicolon);

            return new BrkStatementNode();
        }

        private CasStatementNode ReadCasStatement() {
            this.Read(TokenType.CasKeyword);
            var args = this.ReadArgumentList();
            this.Read(TokenType.Semicolon);

            return new CasStatementNode(args);
        }

        private CpyStatementNode ReadCpyStatement() {
            this.Read(TokenType.CpyKeyword);
            var args = this.ReadArgumentList();
            this.Read(TokenType.Semicolon);

            return new CpyStatementNode(args);
        }

        private DbgStatementNode ReadDbgStatement() {
            this.Read(TokenType.DbgKeyword);
            var args = this.ReadArgumentList();
            this.Read(TokenType.Semicolon);

            return new DbgStatementNode(args);
        }

        private EintStatementNode ReadEintStatement() {
            this.Read(TokenType.EintKeyword);
            this.Read(TokenType.Semicolon);

            return new EintStatementNode();
        }

        private HltStatementNode ReadHltStatement() {
            this.Read(TokenType.HltKeyword);
            this.Read(TokenType.Semicolon);

            return new HltStatementNode();
        }

        private IntdStatementNode ReadIntdStatement() {
            this.Read(TokenType.IntdKeyword);
            this.Read(TokenType.Semicolon);

            return new IntdStatementNode();
        }

        private InteStatementNode ReadInteStatement() {
            this.Read(TokenType.InteKeyword);
            this.Read(TokenType.Semicolon);

            return new InteStatementNode();
        }

        private IntStatementNode ReadIntStatement() {
            this.Read(TokenType.IntKeyword);
            var args = this.ReadArgumentList();
            this.Read(TokenType.Semicolon);

            return new IntStatementNode(args);
        }

        private NopStatementNode ReadNopStatement() {
            this.Read(TokenType.NopKeyword);
            this.Read(TokenType.Semicolon);

            return new NopStatementNode();
        }

        private XchgStatementNode ReadXchgStatement() {
            this.Read(TokenType.XchgKeyword);
            var args = this.ReadArgumentList();
            this.Read(TokenType.Semicolon);

            return new XchgStatementNode(args);
        }

        private IfStatementNode ReadIfStatement() {
            this.Read(TokenType.IfKeyword);
            this.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return new IfStatementNode(exp, block);
        }

        private AssignmentStatementNode ReadAssignmentStatement() {
            var ident = this.ReadIdentifier();
            var op = default(Operator);

            if (this.lexer.Peek(out var t)) {
                switch (t.Type) {
                    case TokenType.PlusEqualsSign: op = Operator.Addition; break;
                    case TokenType.MinusEqualsSign: op = Operator.Subtraction; break;
                    case TokenType.AsteriskEqualsSign: op = Operator.Multiplication; break;
                    case TokenType.ForwardSlashEqualsSign: op = Operator.Division; break;
                    case TokenType.PercentEqualsSign: op = Operator.Remainder; break;
                    case TokenType.CaretEqualsSign: op = Operator.Exponentiation; break;
                    case TokenType.DoubleLessThanEqualsSign: op = Operator.ShiftLeft; break;
                    case TokenType.DoubleGreaterThanEqualsSign: op = Operator.ShiftRight; break;
                    case TokenType.TripleLessThanEqualsSign: op = Operator.RotateLeft; break;
                    case TokenType.TripleGreaterThanEqualsSign: op = Operator.RotateRight; break;
                    case TokenType.AmpersandEqualsSign: op = Operator.And; break;
                    case TokenType.PipeEqualsSign: op = Operator.Or; break;
                    case TokenType.TildeEqualsSign: op = Operator.Xor; break;
                    case TokenType.ExclamationPointAmpersandEqualsSign: op = Operator.NotAnd; break;
                    case TokenType.ExclamationPointPipeEqualsSign: op = Operator.NotOr; break;
                    case TokenType.ExclamationPointTildeEqualsSign: op = Operator.NotXor; break;

                    case TokenType.EqualsSign:
                        this.Read(TokenType.EqualsSign);
                        var exp = this.ReadExpression();
                        this.Read(TokenType.Semicolon);

                        return new AssignmentStatementNode(ident, exp);

                    default:
                        throw this.GetUnexpectedTokenExceptionAtCurrent(t.Type);
                }
            }
            else {
                throw this.GetExpectedTokenExceptionAtCurrent(TokenType.EqualsSign);
            }

            this.lexer.Read(out _);

            var res = new AssignmentStatementNode(ident, new BinaryExpressionNode(ident, new OperatorNode(op), this.ReadExpression()));
            this.Read(TokenType.Semicolon);
            return res;
        }

        private ExpressionNode ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;
            var seenOpenParens = 0;

            while (this.lexer.Peek(out var token)) {
                if (token.Type == TokenType.OpenParenthesis) seenOpenParens++;

                if (start && token.Type == TokenType.Number) {
                    stack.Push(this.ReadNumberLiteral());
                    start = false;
                }
                else if (start && token.Type == TokenType.Identifier) {
                    stack.Push(this.ReadIdentifier());
                    start = false;
                }
                else if (start && (token.Type == TokenType.TrueKeyword || token.Type == TokenType.FalseKeyword)) {
                    stack.Push(this.ReadBooleanLiteral());
                    start = false;
                }
                else if (!start && token.Type == TokenType.CloseParenthesis) {
                    if (--seenOpenParens < 0)
                        break;

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

        private LiteralNode ReadNumberLiteral() => this.lexer.Read(TokenType.Number, out var token) ? new NumberLiteralNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Number);
        private LiteralNode ReadBooleanLiteral() => this.lexer.Read(t => t.Type == TokenType.TrueKeyword || t.Type == TokenType.FalseKeyword, out var token) ? new BooleanLiteralNode(token) : throw this.GetExpectedTokenExceptionAtCurrent("bool");
        private IdentifierNode ReadIdentifier() => this.lexer.Read(TokenType.Identifier, out var token) ? new RegisterNode(token) : throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Identifier);

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
                    case TokenType.DoubleLessThan: res = Operator.ShiftLeft; break;
                    case TokenType.DoubleGreaterThan: res = Operator.ShiftRight; break;
                    case TokenType.TripleLessThan: res = Operator.RotateLeft; break;
                    case TokenType.TripleGreaterThan: res = Operator.RotateRight; break;
                    case TokenType.Ampersand: res = Operator.And; break;
                    case TokenType.Pipe: res = Operator.Or; break;
                    case TokenType.Tilde: res = Operator.Xor; break;
                    case TokenType.ExclamationPointAmpersand: res = Operator.NotAnd; break;
                    case TokenType.ExclamationPointPipe: res = Operator.NotOr; break;
                    case TokenType.ExclamationPointTilde: res = Operator.NotXor; break;
                    case TokenType.DoubleEqualsSign: res = Operator.Equals; break;
                    case TokenType.ExclamationPointEqualsSign: res = Operator.NotEquals; break;
                    case TokenType.LessThan: res = Operator.LessThan; break;
                    case TokenType.LessThanEqualsSign: res = Operator.LessThanOrEqual; break;
                    case TokenType.GreaterThan: res = Operator.GreaterThan; break;
                    case TokenType.GreaterThanEqualsSign: res = Operator.GreaterThanOrEqual; break;
                    case TokenType.OpenParenthesis: res = Operator.OpenParenthesis; break;
                    case TokenType.CloseParenthesis: res = Operator.CloseParenthesis; break;
                    default: throw this.GetUnexpectedTokenExceptionAtCurrent(token.Type);
                }
            }
            else {
                switch (token.Type) {
                    case TokenType.Plus: res = Operator.UnaryPlus; break;
                    case TokenType.Minus: res = Operator.UnaryMinus; break;
                    case TokenType.ExclamationPoint: res = Operator.Not; break;
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
