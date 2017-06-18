using ArkeOS.Tools.KohlCompiler.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private class FileInfo {
            private readonly string contents;
            private readonly int length;
            private int index;
            private int nextNewLine;

            public string Path { get; }
            public int Line { get; private set; }
            public int Column { get; private set; }
            public char Current { get; private set; }

            public FileInfo(string path) {
                this.contents = File.ReadAllText(path);
                this.length = this.contents.Length;
                this.index = 0;
                this.GetNextNewLine();

                this.Path = path;
                this.Line = 1;
                this.Column = 1;
                this.Current = this.contents[0];
            }

            public bool Advance() {
                if (++this.index >= this.length)
                    return true;

                this.Current = this.contents[this.index];
                this.Column++;

                if (this.index == this.nextNewLine) {
                    this.Column = 1;
                    this.Line++;
                    this.GetNextNewLine();
                }

                return false;
            }

            private void GetNextNewLine() { this.nextNewLine = this.contents.IndexOf(Environment.NewLine, this.nextNewLine, StringComparison.Ordinal); if (this.nextNewLine == -1) this.nextNewLine += Environment.NewLine.Length; }
        }

        private readonly Queue<FileInfo> files;
        private FileInfo file;
        private Token currentToken;

        public bool AtEnd { get; private set; }
        public PositionInfo CurrentPosition { get; private set; }

        public Lexer(IReadOnlyList<string> filePaths) {
            this.files = new Queue<FileInfo>(filePaths.Select(f => new FileInfo(f)));
            this.file = this.files.Dequeue();
            this.currentToken = this.LexNextToken();

            this.AtEnd = false;
        }

        private void Advance() {
            if (this.file.Advance())
                this.file = this.files.Any() ? this.files.Dequeue() : null;
        }

        private bool PeekChar(out char value) {
            value = this.file?.Current ?? default(char);

            return this.file != null;
        }

        private Token ReadIdentifier() {
            var res = new StringBuilder();

            while (this.PeekChar(out var c) && (char.IsLetterOrDigit(c) || c == '_')) {
                this.Advance();

                res.Append(c);
            }

            return new Token(TokenType.Identifier, res.ToString());
        }

        private Token ReadNumber() {
            var res = new StringBuilder();
            var radix = 10;

            if (this.PeekChar(out var s) && s == '0') {
                this.Advance();

                if (this.PeekChar(out var c)) {
                    switch (c) {
                        case 'd': radix = 10; this.Advance(); break;
                        case 'x': radix = 16; this.Advance(); break;
                        case 'b': radix = 2; this.Advance(); break;
                        case 'o': radix = 8; this.Advance(); break;
                        default: res.Append(s); break;
                    }
                }
                else {
                    res.Append(s);
                }
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.PeekChar(out var c) && (valid.Contains(c) || c == '_')) {
                this.Advance();

                if (c != '_')
                    res.Append(c);
            }

            return new Token(TokenType.Number, Convert.ToUInt64(res.ToString(), radix).ToString());
        }

        private Token ReadWhitespace() {
            var res = new StringBuilder();

            while (this.PeekChar(out var c) && char.IsWhiteSpace(c)) {
                this.Advance();

                res.Append(c);
            }

            return new Token(TokenType.Whitespace, res.ToString());
        }

        private Token ReadSymbol(char c) {
            var res = default(Token);

            switch (c) {
                case '+': res = new Token(TokenType.Plus, c); break;
                case '-': res = new Token(TokenType.Minus, c); break;
                case '*': res = new Token(TokenType.Asterisk, c); break;
                case '/': res = new Token(TokenType.ForwardSlash, c); break;
                case '^': res = new Token(TokenType.Caret, c); break;
                case '%': res = new Token(TokenType.Percent, c); break;
                case '(': res = new Token(TokenType.OpenParenthesis, c); break;
                case ')': res = new Token(TokenType.CloseParenthesis, c); break;
                case '=': res = new Token(TokenType.EqualsSign, c); break;
                case ';': res = new Token(TokenType.Semicolon, c); break;
                default: throw new UnexpectedCharacterException(this.CurrentPosition, c);
            }

            this.Advance();

            return res;
        }

        private Token LexNextToken() {
            if (!this.PeekChar(out var c)) {
                this.AtEnd = true;

                return default(Token);
            }

            this.CurrentPosition = new PositionInfo(this.file.Path, this.file.Line, this.file.Column);

            if (char.IsLetter(c)) {
                return this.ReadIdentifier();
            }
            else if (char.IsNumber(c)) {
                return this.ReadNumber();
            }
            else if (char.IsWhiteSpace(c)) {
                _ = this.ReadWhitespace();

                return this.LexNextToken();
            }
            else {
                return this.ReadSymbol(c);
            }
        }

        public List<Token> ToList() {
            var res = new List<Token>();

            while (this.Read(out var t))
                res.Add(t);

            return res;
        }

        public bool Peek(out Token token) {
            token = this.currentToken;

            return !this.AtEnd;
        }

        public bool Read(out Token token) {
            token = this.currentToken;

            var res = this.AtEnd;

            this.currentToken = this.LexNextToken();

            return !res;
        }

        public bool Read(TokenType type) => this.Read(type, out _);
        public bool Read(TokenType type, out Token token) => this.Read(t => t.Type == type, out token);
        public bool Read(Func<Token, bool> validator, out Token token) => this.Peek(out token) && validator(token) && this.Read(out _);
    }
}
