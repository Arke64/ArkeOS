namespace ArkeOS.Tools.KohlCompiler {
    public enum TokenType {
        IntegerLiteral,
        FloatLiteral,
        BoolLiteral,
        NullLiteral,

        Identifier,

        Whitespace,

        IfKeyword,

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

        OpenParenthesis,
        CloseParenthesis,

        OpenCurlyBrace,
        CloseCurlyBrace,

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

    public struct Token {
        public TokenType Type;
        public string Value;

        public Token(TokenType type) : this(type, string.Empty) { }
        public Token(TokenType type, string value) => (this.Type, this.Value) = (type, value);

        public override string ToString() => $"{this.Type}{(this.Value != string.Empty ? ": " + this.Value : string.Empty)}";

        public bool IsOperator() {
            switch (this.Type) {
                case TokenType.OpenParenthesis:
                case TokenType.CloseParenthesis:

                case TokenType.Equal:
                case TokenType.DoubleEqual:

                case TokenType.ExclamationPoint:
                case TokenType.ExclamationPointEqual:

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

                case TokenType.LessThanEqual:
                case TokenType.GreaterThanEqual:
                    return true;
            }

            return false;
        }
    }
}
