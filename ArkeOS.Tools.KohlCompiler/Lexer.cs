using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private readonly TokenStream stream;
        private readonly string source;
        private readonly int length;
        private int index;
        private Token current;
        private bool currentValid;

        public Lexer(string source) {
            this.stream = new TokenStream(this);
            this.source = source;
            this.length = source.Length;
            this.index = 0;

            this.LexNextToken();
        }

        private void Advance() => this.index++;

        private bool Peek(out char value) {
            if (this.index < this.length) {
                value = this.source[this.index];

                return true;
            }
            else {
                value = default(char);

                return false;
            }
        }

        private string ReadIdentifier() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && (char.IsLetterOrDigit(c) || c == '_')) {
                this.Advance();

                res.Append(c);
            }

            return res.ToString();
        }

        private string ReadNumber() {
            var res = new StringBuilder();
            var radix = 10;

            if (this.Peek(out var s) && s == '0') {
                this.Advance();

                if (this.Peek(out var c)) {
                    switch (c) {
                        case 'd': radix = 10; this.Advance(); break;
                        case 'x': radix = 16; this.Advance(); break;
                        case 'b': radix = 2; this.Advance(); break;
                        case 'o': radix = 8; this.Advance(); break;
                        default: res.Append(s); break;
                    }
                }
                else {
                    return "0";
                }
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.Peek(out var c) && (valid.Contains(c) || c == '_')) {
                this.Advance();

                if (c != '_')
                    res.Append(c);
            }

            return Convert.ToUInt64(res.ToString(), radix).ToString();
        }

        private string ReadWhitespace() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && char.IsWhiteSpace(c)) {
                this.Advance();

                res.Append(c);
            }

            return res.ToString();
        }

        private void LexNextToken() {
            if (this.Peek(out var c)) {
                if (char.IsLetter(c)) {
                    this.current = new Token(TokenType.Identifier, this.ReadIdentifier());
                }
                else if (char.IsNumber(c)) {
                    this.current = new Token(TokenType.Number, this.ReadNumber());
                }
                else if (char.IsWhiteSpace(c)) {
                    this.ReadWhitespace();

                    this.LexNextToken();

                    return;
                }
                else {
                    switch (c) {
                        case '+': this.current = new Token(TokenType.Plus, c); break;
                        case '-': this.current = new Token(TokenType.Minus, c); break;
                        case '*': this.current = new Token(TokenType.Asterisk, c); break;
                        case '/': this.current = new Token(TokenType.ForwardSlash, c); break;
                        case '^': this.current = new Token(TokenType.Caret, c); break;
                        case '%': this.current = new Token(TokenType.Percent, c); break;
                        case '(': this.current = new Token(TokenType.OpenParenthesis, c); break;
                        case ')': this.current = new Token(TokenType.CloseParenthesis, c); break;
                        case '=': this.current = new Token(TokenType.EqualsSign, c); break;
                        case ';': this.current = new Token(TokenType.Semicolon, c); break;
                        default: throw new InvalidOperationException($"Unexpected '{c}'");
                    }

                    this.Advance();
                }

                this.currentValid = true;
            }
            else {
                this.current = default(Token);
                this.currentValid = false;
            }
        }

        public bool ReadNext(out Token token) {
            var res = this.PeekNext(out token);

            if (res)
                this.LexNextToken();

            return res;
        }

        public bool PeekNext(out Token token) {
            token = this.current;

            return this.currentValid;
        }

        public TokenStream GetStream() => this.stream;
    }
}