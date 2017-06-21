namespace ArkeOS.Tools.KohlCompiler {
    public enum TokenType {
        Number,
        Identifier,
        Whitespace,
        Equal,
        DoubleEqual,
        ExclamationPoint,
        Ampersand,
        Pipe,
        Tilde,
        AmpersandEqual,
        PipeEqual,
        TildeEqual,
        ExclamationPointAmpersand,
        ExclamationPointPipe,
        ExclamationPointTilde,
        ExclamationPointAmpersandEqual,
        ExclamationPointPipeEqual,
        ExclamationPointTildeEqual,
        ExclamationPointEqual,
        LessThan,
        LessThanEqual,
        DoubleLessThan,
        DoubleLessThanEqual,
        TripleLessThan,
        TripleLessThanEqual,
        GreaterThan,
        GreaterThanEqual,
        DoubleGreaterThan,
        DoubleGreaterThanEqual,
        TripleGreaterThan,
        TripleGreaterThanEqual,
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Caret,
        Percent,
        PlusEqual,
        MinusEqual,
        AsteriskEqual,
        ForwardSlashEqual,
        CaretEqual,
        PercentEqual,
        Semicolon,
        Comma,
        Period,
        OpenParenthesis,
        CloseParenthesis,
        OpenCurlyBrace,
        CloseCurlyBrace,
        TrueKeyword,
        FalseKeyword,
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
    }

    public struct Token {
        public TokenType Type;
        public string Value;

        public Token(TokenType type) : this(type, string.Empty) { }
        public Token(TokenType type, string value) => (this.Type, this.Value) = (type, value);

        public override string ToString() => $"{this.Type}{(this.Type == TokenType.Number || this.Type == TokenType.Identifier ? ": " + this.Value : string.Empty)}";

        public bool IsOperator() {
            switch (this.Type) {
                case TokenType.Equal:
                case TokenType.DoubleEqual:
                case TokenType.ExclamationPoint:
                case TokenType.Ampersand:
                case TokenType.Pipe:
                case TokenType.Tilde:
                case TokenType.ExclamationPointAmpersand:
                case TokenType.ExclamationPointPipe:
                case TokenType.ExclamationPointTilde:
                case TokenType.ExclamationPointEqual:
                case TokenType.LessThan:
                case TokenType.LessThanEqual:
                case TokenType.DoubleLessThan:
                case TokenType.TripleLessThan:
                case TokenType.GreaterThan:
                case TokenType.GreaterThanEqual:
                case TokenType.DoubleGreaterThan:
                case TokenType.TripleGreaterThan:
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Caret:
                case TokenType.Percent:
                case TokenType.OpenParenthesis:
                case TokenType.CloseParenthesis:
                    return true;
            }

            return false;
        }
    }
}
