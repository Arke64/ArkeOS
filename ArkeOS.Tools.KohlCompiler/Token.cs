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
        ExclamationPointAmpersand,
        ExclamationPointPipe,
        ExclamationPointTilde,
        ExclamationPointEqualsSign,
        LessThan,
        LessThanEqualsSign,
        DoubleLessThan,
        TripleLessThan,
        GreaterThan,
        GreaterThanEqualsSign,
        DoubleGreaterThan,
        TripleGreaterThan,
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Caret,
        Percent,
        Semicolon,
        Comma,
        Period,
        OpenParenthesis,
        CloseParenthesis,
        OpenCurlyBrace,
        CloseCurlyBrace,
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
    }
}
