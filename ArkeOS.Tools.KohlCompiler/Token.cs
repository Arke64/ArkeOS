namespace ArkeOS.Tools.KohlCompiler {
    public enum TokenType {
        Number,
        Identifier,
        Asterisk,
        Plus,
        Minus,
        ForwardSlash,
        Percent,
        Caret,
        OpenParenthesis,
        CloseParenthesis,
        EqualsSign,
        Semicolon,
        Whitespace,
        OpenCurlyBrace,
        CloseCurlyBrace,
        IfKeyword,
    }

    public struct Token {
        public TokenType Type;
        public string Value;

        public Token(TokenType type, string value) => (this.Type, this.Value) = (type, value);
        public Token(TokenType type, char value) : this(type, value.ToString()) { }

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
