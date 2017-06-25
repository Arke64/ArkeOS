using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;

namespace ArkeOS.Tools.KohlCompiler {
    public class Parser {
        private readonly Lexer lexer;

        public Parser(Lexer lexer) => this.lexer = lexer;

        public ProgramNode Parse() => new ProgramNode(this.ReadStatementBlock());

        private StatementBlockNode ReadStatementBlock() {
            var block = new StatementBlockNode();

            if (this.lexer.TryRead(TokenType.OpenCurlyBrace)) {
                while (!this.lexer.TryRead(TokenType.CloseCurlyBrace))
                    block.Add(this.ReadStatement());
            }
            else {
                block.Add(this.ReadStatement());
            }

            return block;
        }

        private StatementNode ReadStatement() {
            var tok = this.lexer.Peek();
            var res = default(StatementNode);

            switch (tok.Class) {
                case TokenClass.BlockKeyword: return this.ReadBlockStatement();
                case TokenClass.IntrinsicKeyword: res = this.ReadIntrinsicStatement(); break;
                case TokenClass.LValue: res = this.ReadAssignmentStatement(); break;
                case TokenClass.Separator when tok.Type == TokenType.Semicolon: res = new EmptyStatementNode(); break;
                default: throw this.GetUnexpectedException(tok.Type);
            }

            this.lexer.Read(TokenType.Semicolon);

            return res;
        }

        private StatementNode ReadBlockStatement() {
            var tok = this.lexer.Peek();

            switch (tok.Type) {
                case TokenType.IfKeyword: return this.ReadIfStatement();
                default: throw this.GetUnexpectedException(tok.Type);
            }
        }

        private StatementNode ReadIntrinsicStatement() {
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
                default: throw this.GetUnexpectedException(tok.Type);
            }
        }

        private AssignmentStatementNode ReadAssignmentStatement() {
            var target = this.ReadLValue();
            var op = this.lexer.Read(TokenClass.Assignment);
            var exp = this.ReadExpression();

            return op.Type == TokenType.Equal ? new AssignmentStatementNode(target, exp) : new CompoundAssignmentStatementNode(target, OperatorNode.FromCompoundToken(op) ?? throw this.GetUnexpectedException(op.Type), exp);
        }

        private IfStatementNode ReadIfStatement() {
            this.lexer.Read(TokenType.IfKeyword);
            this.lexer.Read(TokenType.OpenParenthesis);
            var exp = this.ReadExpression();
            this.lexer.Read(TokenType.CloseParenthesis);
            var block = this.ReadStatementBlock();

            return new IfStatementNode(exp, block);
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

        private ExpressionNode ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;
            var seenOpenParens = 0;

            while (true) {
                var tok = this.lexer.Peek();

                if (tok.Type == TokenType.OpenParenthesis) seenOpenParens++;

                if (start && (tok.Class == TokenClass.RValue || tok.Class == TokenClass.LValue)) {
                    stack.Push(this.ReadRValue());
                    start = false;
                }
                else if (!start && tok.Type == TokenType.CloseParenthesis) {
                    if (--seenOpenParens < 0)
                        break;

                    stack.Push(this.ReadOperator(OperatorClass.Binary), this.lexer.CurrentPosition);
                    start = false;
                }
                else if (tok.Class == TokenClass.Operator) {
                    stack.Push(this.ReadOperator(start && tok.Type != TokenType.OpenParenthesis ? OperatorClass.Unary : OperatorClass.Binary), this.lexer.CurrentPosition);
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode() ?? throw this.GetExpectedException("expression");
        }

        private OperatorNode ReadOperator(OperatorClass cls) {
            var token = this.lexer.Read(TokenClass.Operator);

            return OperatorNode.FromToken(token, cls) ?? throw this.GetUnexpectedException(token.Type);
        }

        private RValueNode ReadRValue() {
            var tok = this.lexer.Read();

            switch (tok.Type) {
                case TokenType.IntegerLiteral: return new IntegerLiteralNode(tok);
                case TokenType.BoolLiteral: return new BoolLiteralNode(tok);
                case TokenType.Identifier: return new RegisterNode(tok);
                default: throw this.GetUnexpectedException(tok.Type);
            }
        }

        private LValueNode ReadLValue() => new RegisterNode(this.lexer.Read());

        private UnexpectedException GetUnexpectedException(TokenType t) => new UnexpectedException(this.lexer.CurrentPosition, t);
        private ExpectedException GetExpectedException(string t) => new ExpectedException(this.lexer.CurrentPosition, t);
    }
}
