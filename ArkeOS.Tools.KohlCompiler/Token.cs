using System;

namespace ArkeOS.Tools.KohlCompiler {
    public enum TokenType {
        IntegerLiteral,
        FloatLiteral,
        BoolLiteral,
        NullLiteral,

        Identifier,

        Whitespace,

        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        FuncKeyword,
        VarKeyword,
        ConstKeyword,
        ReturnKeyword,

        DbgKeyword,
        BrkKeyword,
        HltKeyword,
        NopKeyword,
        IntKeyword,
        EintKeyword,
        InteKeyword,
        IntdKeyword,
        XchgKeyword,
        CasKeyword,
        CpyKeyword,

        Semicolon,
        Comma,
        Period,
        Colon,

        OpenParenthesis,
        CloseParenthesis,

        OpenCurlyBrace,
        CloseCurlyBrace,

        OpenSquareBrace,
        CloseSquareBrace,

        Equal,
        DoubleEqual,

        ExclamationPoint,
        ExclamationPointEqual,

        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Caret,
        Percent,
        Ampersand,
        Pipe,
        Tilde,
        ExclamationPointAmpersand,
        ExclamationPointPipe,
        ExclamationPointTilde,
        LessThan,
        DoubleLessThan,
        TripleLessThan,
        GreaterThan,
        DoubleGreaterThan,
        TripleGreaterThan,

        PlusEqual,
        MinusEqual,
        AsteriskEqual,
        ForwardSlashEqual,
        CaretEqual,
        PercentEqual,
        AmpersandEqual,
        PipeEqual,
        TildeEqual,
        ExclamationPointAmpersandEqual,
        ExclamationPointPipeEqual,
        ExclamationPointTildeEqual,
        LessThanEqual,
        DoubleLessThanEqual,
        TripleLessThanEqual,
        GreaterThanEqual,
        DoubleGreaterThanEqual,
        TripleGreaterThanEqual,
    }

    public enum TokenClass {
        Operator,
        Assignment,
        Literal,
        Identifier,
        BlockKeyword,
        StatementKeyword,
        IntrinsicKeyword,
        Brace,
        Separator,
        Whitespace,
    }

    public struct Token {
        private bool classSet;
        private TokenClass tokenClass;

        public PositionInfo Position;
        public TokenType Type;
        public string Value;

        public Token(PositionInfo position, TokenType type) : this(position, type, string.Empty) { }
        public Token(PositionInfo position, TokenType type, string value) => (this.Position, this.Type, this.Value, this.classSet, this.tokenClass) = (position, type, value, false, default(TokenClass));

        public override string ToString() => $"{this.Type}{(this.Value != string.Empty ? ": " + this.Value : string.Empty)}";

        public TokenClass Class {
            get {
                if (!this.classSet) {
                    switch (this.Type) {
                        case TokenType.OpenParenthesis:
                        case TokenType.CloseParenthesis:

                        case TokenType.DoubleEqual:
                        case TokenType.ExclamationPoint:
                        case TokenType.ExclamationPointEqual:

                        case TokenType.LessThanEqual:
                        case TokenType.GreaterThanEqual:

                        case TokenType.Plus:
                        case TokenType.Minus:
                        case TokenType.Asterisk:
                        case TokenType.ForwardSlash:
                        case TokenType.Caret:
                        case TokenType.Percent:
                        case TokenType.Ampersand:
                        case TokenType.Pipe:
                        case TokenType.Tilde:

                        case TokenType.ExclamationPointAmpersand:
                        case TokenType.ExclamationPointPipe:
                        case TokenType.ExclamationPointTilde:
                        case TokenType.LessThan:
                        case TokenType.DoubleLessThan:
                        case TokenType.TripleLessThan:
                        case TokenType.GreaterThan:
                        case TokenType.DoubleGreaterThan:
                        case TokenType.TripleGreaterThan:
                            this.tokenClass = TokenClass.Operator;
                            break;

                        case TokenType.Equal:

                        case TokenType.PlusEqual:
                        case TokenType.MinusEqual:
                        case TokenType.AsteriskEqual:
                        case TokenType.ForwardSlashEqual:
                        case TokenType.CaretEqual:
                        case TokenType.PercentEqual:
                        case TokenType.AmpersandEqual:
                        case TokenType.PipeEqual:
                        case TokenType.TildeEqual:

                        case TokenType.ExclamationPointAmpersandEqual:
                        case TokenType.ExclamationPointPipeEqual:
                        case TokenType.ExclamationPointTildeEqual:
                        case TokenType.DoubleLessThanEqual:
                        case TokenType.TripleLessThanEqual:
                        case TokenType.DoubleGreaterThanEqual:
                        case TokenType.TripleGreaterThanEqual:
                            this.tokenClass = TokenClass.Assignment;
                            break;

                        case TokenType.IntegerLiteral:
                        case TokenType.FloatLiteral:
                        case TokenType.BoolLiteral:
                        case TokenType.NullLiteral:
                            this.tokenClass = TokenClass.Literal;
                            break;

                        case TokenType.Identifier:
                            this.tokenClass = TokenClass.Identifier;
                            break;

                        case TokenType.IfKeyword:
                        case TokenType.ElseKeyword:
                        case TokenType.WhileKeyword:
                        case TokenType.FuncKeyword:
                            this.tokenClass = TokenClass.BlockKeyword;
                            break;

                        case TokenType.VarKeyword:
                        case TokenType.ConstKeyword:
                        case TokenType.ReturnKeyword:
                            this.tokenClass = TokenClass.StatementKeyword;
                            break;

                        case TokenType.DbgKeyword:
                        case TokenType.BrkKeyword:
                        case TokenType.HltKeyword:
                        case TokenType.NopKeyword:
                        case TokenType.IntKeyword:
                        case TokenType.EintKeyword:
                        case TokenType.InteKeyword:
                        case TokenType.IntdKeyword:
                        case TokenType.XchgKeyword:
                        case TokenType.CasKeyword:
                        case TokenType.CpyKeyword:
                            this.tokenClass = TokenClass.IntrinsicKeyword;
                            break;

                        case TokenType.Semicolon:
                        case TokenType.Comma:
                        case TokenType.Period:
                        case TokenType.Colon:
                            this.tokenClass = TokenClass.Separator;
                            break;

                        case TokenType.OpenCurlyBrace:
                        case TokenType.CloseCurlyBrace:
                        case TokenType.OpenSquareBrace:
                        case TokenType.CloseSquareBrace:
                            this.tokenClass = TokenClass.Brace;
                            break;

                        case TokenType.Whitespace:
                            this.tokenClass = TokenClass.Whitespace;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }

                    this.classSet = true;
                }

                return this.tokenClass;
            }
        }
    }
}
