namespace ArkeOS.Tools.KohlCompiler {
    public enum TokenType {
        Number,
        Identifier,
        Whitespace,
        EqualsSign,
        DoubleEqualsSign,
        ExclamationPoint,
        Ampersand,
        Pipe,
        Tilde,
        AmpersandEqualsSign,
        PipeEqualsSign,
        TildeEqualsSign,
        ExclamationPointAmpersand,
        ExclamationPointPipe,
        ExclamationPointTilde,
        ExclamationPointAmpersandEqualsSign,
        ExclamationPointPipeEqualsSign,
        ExclamationPointTildeEqualsSign,
        ExclamationPointEqualsSign,
        LessThan,
        LessThanEqualsSign,
        DoubleLessThan,
        DoubleLessThanEqualsSign,
        TripleLessThan,
        TripleLessThanEqualsSign,
        GreaterThan,
        GreaterThanEqualsSign,
        DoubleGreaterThan,
        DoubleGreaterThanEqualsSign,
        TripleGreaterThan,
        TripleGreaterThanEqualsSign,
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Caret,
        Percent,
        PlusEqualsSign,
        MinusEqualsSign,
        AsteriskEqualsSign,
        ForwardSlashEqualsSign,
        CaretEqualsSign,
        PercentEqualsSign,
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

        public Token(TokenType type, string value) => (this.Type, this.Value) = (type, value);
        public Token(TokenType type, char value) : this(type, value.ToString()) { }
        public Token(TokenType type, params char[] value) : this(type, new string(value)) { }

        public override string ToString() => $"{this.Type}{(this.Type == TokenType.Number || this.Type == TokenType.Identifier ? ": " + this.Value : string.Empty)}";

        public bool IsOperator() {
            switch (this.Type) {
                case TokenType.EqualsSign:
                case TokenType.DoubleEqualsSign:
                case TokenType.ExclamationPoint:
                case TokenType.Ampersand:
                case TokenType.Pipe:
                case TokenType.Tilde:
                case TokenType.ExclamationPointAmpersand:
                case TokenType.ExclamationPointPipe:
                case TokenType.ExclamationPointTilde:
                case TokenType.ExclamationPointEqualsSign:
                case TokenType.LessThan:
                case TokenType.LessThanEqualsSign:
                case TokenType.DoubleLessThan:
                case TokenType.TripleLessThan:
                case TokenType.GreaterThan:
                case TokenType.GreaterThanEqualsSign:
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
