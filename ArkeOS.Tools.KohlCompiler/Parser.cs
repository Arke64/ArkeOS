using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;

namespace ArkeOS.Tools.KohlCompiler {
    public class Parser {
        private readonly Lexer lexer;

        public Parser(Lexer lexer) => this.lexer = lexer;

        public ProgramNode Parse() {
            var res = new ProgramNode(this.ReadStatementBlock());

            if (this.lexer.TryPeek(out var t))
                throw this.GetUnexpectedTokenExceptionAtCurrent(t.Type);

            return res;
        }

        private StatementBlockNode ReadStatementBlock() {
            var block = new StatementBlockNode();

            if (this.lexer.TryPeek(out var t)) {
                if (t.Type == TokenType.OpenCurlyBrace) {
                    this.lexer.Read(TokenType.OpenCurlyBrace);

                    while (this.lexer.TryPeek(out t) && t.Type != TokenType.CloseCurlyBrace)
                        block.Add(this.ReadStatement());

                    this.lexer.Read(TokenType.CloseCurlyBrace);
                }
                else {
                    block.Add(this.ReadStatement());
                }
            }

            return block;
        }

        private StatementNode ReadStatement() {
            var res = default(StatementNode);

            if (this.lexer.TryPeek(out var t)) {
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

            this.lexer.Read(TokenType.OpenParenthesis);

            if (!this.lexer.TryRead(TokenType.CloseParenthesis)) {
                do {
                    list.Add(this.ReadExpression());
                } while (this.lexer.TryRead(TokenType.Comma));

                this.lexer.Read(TokenType.CloseParenthesis);
            }

            return list;
        }

        private BrkStatementNode ReadBrkStatement() {
            this.lexer.Read(TokenType.BrkKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new BrkStatementNode();
        }

        private CasStatementNode ReadCasStatement() {
            this.lexer.Read(TokenType.CasKeyword);
            var args = this.ReadArgumentList();
            this.lexer.Read(TokenType.Semicolon);

            return new CasStatementNode(args);
        }

        private CpyStatementNode ReadCpyStatement() {
            this.lexer.Read(TokenType.CpyKeyword);
            var args = this.ReadArgumentList();
            this.lexer.Read(TokenType.Semicolon);

            return new CpyStatementNode(args);
        }

        private DbgStatementNode ReadDbgStatement() {
            this.lexer.Read(TokenType.DbgKeyword);
            var args = this.ReadArgumentList();
            this.lexer.Read(TokenType.Semicolon);

            return new DbgStatementNode(args);
        }

        private EintStatementNode ReadEintStatement() {
            this.lexer.Read(TokenType.EintKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new EintStatementNode();
        }

        private HltStatementNode ReadHltStatement() {
            this.lexer.Read(TokenType.HltKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new HltStatementNode();
        }

        private IntdStatementNode ReadIntdStatement() {
            this.lexer.Read(TokenType.IntdKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new IntdStatementNode();
        }

        private InteStatementNode ReadInteStatement() {
            this.lexer.Read(TokenType.InteKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new InteStatementNode();
        }

        private IntStatementNode ReadIntStatement() {
            this.lexer.Read(TokenType.IntKeyword);
            var args = this.ReadArgumentList();
            this.lexer.Read(TokenType.Semicolon);

            return new IntStatementNode(args);
        }

        private NopStatementNode ReadNopStatement() {
            this.lexer.Read(TokenType.NopKeyword);
            this.lexer.Read(TokenType.Semicolon);

            return new NopStatementNode();
        }

        private XchgStatementNode ReadXchgStatement() {
            this.lexer.Read(TokenType.XchgKeyword);
            var args = this.ReadArgumentList();
            this.lexer.Read(TokenType.Semicolon);

            return new XchgStatementNode(args);
        }

        private IfStatementNode ReadIfStatement() {
            this.lexer.Read(TokenType.IfKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return new IfStatementNode(exp, block);
        }

        private AssignmentStatementNode ReadAssignmentStatement() {
            var ident = this.ReadIdentifier();
            var op = default(Operator);

            if (this.lexer.TryPeek(out var t)) {
                switch (t.Type) {
                    case TokenType.PlusEqual: op = Operator.Addition; break;
                    case TokenType.MinusEqual: op = Operator.Subtraction; break;
                    case TokenType.AsteriskEqual: op = Operator.Multiplication; break;
                    case TokenType.ForwardSlashEqual: op = Operator.Division; break;
                    case TokenType.PercentEqual: op = Operator.Remainder; break;
                    case TokenType.CaretEqual: op = Operator.Exponentiation; break;
                    case TokenType.DoubleLessThanEqual: op = Operator.ShiftLeft; break;
                    case TokenType.DoubleGreaterThanEqual: op = Operator.ShiftRight; break;
                    case TokenType.TripleLessThanEqual: op = Operator.RotateLeft; break;
                    case TokenType.TripleGreaterThanEqual: op = Operator.RotateRight; break;
                    case TokenType.AmpersandEqual: op = Operator.And; break;
                    case TokenType.PipeEqual: op = Operator.Or; break;
                    case TokenType.TildeEqual: op = Operator.Xor; break;
                    case TokenType.ExclamationPointAmpersandEqual: op = Operator.NotAnd; break;
                    case TokenType.ExclamationPointPipeEqual: op = Operator.NotOr; break;
                    case TokenType.ExclamationPointTildeEqual: op = Operator.NotXor; break;

                    case TokenType.Equal:
                        this.lexer.Read(TokenType.Equal);
                        var exp = this.ReadExpression();
                        this.lexer.Read(TokenType.Semicolon);

                        return new AssignmentStatementNode(ident, exp);

                    default:
                        throw this.GetUnexpectedTokenExceptionAtCurrent(t.Type);
                }
            }
            else {
                throw this.GetExpectedTokenExceptionAtCurrent(TokenType.Equal);
            }

            this.lexer.TryRead(out _);

            var res = new AssignmentStatementNode(ident, new BinaryExpressionNode(ident, new OperatorNode(op), this.ReadExpression()));
            this.lexer.Read(TokenType.Semicolon);
            return res;
        }

        private ExpressionNode ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;
            var seenOpenParens = 0;

            while (this.lexer.TryPeek(out var token)) {
                if (token.Type == TokenType.OpenParenthesis) seenOpenParens++;

                if (start && token.Type == TokenType.Number) {
                    stack.Push(this.ReadNumberLiteral());
                    start = false;
                }
                else if (start && token.Type == TokenType.Identifier) {
                    stack.Push(this.ReadIdentifier());
                    start = false;
                }
                else if (start && token.Type == TokenType.TrueKeyword) {
                    stack.Push(this.ReadTrueLiteral());
                    start = false;
                }
                else if (start && token.Type == TokenType.FalseKeyword) {
                    stack.Push(this.ReadFalseLiteral());
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

        private IdentifierNode ReadIdentifier() => new RegisterNode(this.lexer.Read(TokenType.Identifier));
        private LiteralNode ReadNumberLiteral() => new NumberLiteralNode(this.lexer.Read(TokenType.Number));
        private LiteralNode ReadTrueLiteral() => new BooleanLiteralNode(this.lexer.Read(TokenType.TrueKeyword));
        private LiteralNode ReadFalseLiteral() => new BooleanLiteralNode(this.lexer.Read(TokenType.FalseKeyword));

        private OperatorNode ReadOperator(bool unary) {
            if (!this.lexer.TryRead(t => t.IsOperator(), out var token))
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
                    case TokenType.DoubleEqual: res = Operator.Equals; break;
                    case TokenType.ExclamationPointEqual: res = Operator.NotEquals; break;
                    case TokenType.LessThan: res = Operator.LessThan; break;
                    case TokenType.LessThanEqual: res = Operator.LessThanOrEqual; break;
                    case TokenType.GreaterThan: res = Operator.GreaterThan; break;
                    case TokenType.GreaterThanEqual: res = Operator.GreaterThanOrEqual; break;
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
