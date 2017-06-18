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
    }

    public struct Token {
        public PositionInfo Position;
        public TokenType Type;
        public string Value;

        public Token(PositionInfo position, TokenType type, string value) => (this.Position, this.Type, this.Value) = (position, type, value);
        public Token(PositionInfo position, TokenType type, char value) : this(position, type, value.ToString()) { }

        public override string ToString() => $"{this.Type}{(this.Type == TokenType.Number || this.Type == TokenType.Identifier ? ": " + this.Value : string.Empty)}";
    }
}
