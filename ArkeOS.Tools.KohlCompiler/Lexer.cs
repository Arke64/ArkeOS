using ArkeOS.Tools.KohlCompiler.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private static IReadOnlyDictionary<string, TokenType> Keywords { get; } = new Dictionary<string, TokenType> {
            ["true"] = TokenType.TrueKeyword,
            ["false"] = TokenType.FalseKeyword,
            ["if"] = TokenType.IfKeyword,
            ["dbg"] = TokenType.DbgKeyword,
            ["brk"] = TokenType.BrkKeyword,
            ["hlt"] = TokenType.HltKeyword,
            ["nop"] = TokenType.NopKeyword,
            ["int"] = TokenType.IntKeyword,
            ["eint"] = TokenType.EintKeyword,
            ["inte"] = TokenType.InteKeyword,
            ["intd"] = TokenType.IntdKeyword,
            ["xchg"] = TokenType.XchgKeyword,
            ["cas"] = TokenType.CasKeyword,
            ["cpy"] = TokenType.CpyKeyword
        };

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

            private void GetNextNewLine() { this.nextNewLine = this.contents.IndexOf(Environment.NewLine, this.nextNewLine, StringComparison.Ordinal); if (this.nextNewLine != -1) this.nextNewLine += Environment.NewLine.Length; }
        }

        private readonly Queue<FileInfo> files;
        private FileInfo file;
        private bool eof;
        private Token currentToken;

        public PositionInfo CurrentPosition => this.file != null ? new PositionInfo(this.file.Path, this.file.Line, this.file.Column) : default(PositionInfo);

        public Lexer(IReadOnlyList<string> filePaths) {
            this.files = new Queue<FileInfo>(filePaths.Select(f => new FileInfo(f)));
            this.file = this.files.Dequeue();
            this.eof = false;
            this.currentToken = this.LexNextToken();
        }

        public bool TryPeekChar(out char chr) {
            if (this.file != null) {
                chr = this.file.Current;
                return true;
            }
            else {
                chr = default(char);
                return false;
            }
        }

        public bool TryReadChar(out char chr) {
            var res = this.TryPeekChar(out chr);

            if (this.file.Advance())
                this.file = this.files.Any() ? this.files.Dequeue() : null;

            return res;
        }

        public char ReadChar(char chr) => this.TryReadChar(chr) ? chr : throw new ExpectedCharacterException(this.CurrentPosition, chr);
        public bool TryReadChar(char chr) => this.TryReadChar(c => c == chr, out _);
        public bool TryReadChar(Predicate<char> predicate, out char chr) => this.TryPeekChar(predicate, out chr) && this.TryReadChar(out _);

        public char PeekChar(char chr) => this.TryPeekChar(chr) ? chr : throw new ExpectedCharacterException(this.CurrentPosition, chr);
        public bool TryPeekChar(char chr) => this.TryPeekChar(c => c == chr, out _);
        public bool TryPeekChar(Predicate<char> predicate, out char chr) => this.TryPeekChar(out chr) && predicate(chr);

        private Token ReadWhitespace() {
            var res = new StringBuilder();

            while (this.TryReadChar(char.IsWhiteSpace, out var c))
                res.Append(c);

            return new Token(TokenType.Whitespace, res.ToString());
        }

        private Token ReadWord() {
            var res = new StringBuilder();

            while (this.TryReadChar(chr => char.IsLetterOrDigit(chr) || chr == '_', out var c))
                res.Append(c);

            var str = res.ToString();

            return new Token(Lexer.Keywords.TryGetValue(str, out var t) ? t : TokenType.Identifier, str);
        }

        private Token ReadNumber() {
            var res = new StringBuilder();
            var radix = 10;

            if (this.TryReadChar('0')) {
                if (this.TryReadChar('d')) radix = 10;
                else if (this.TryReadChar('x')) radix = 16;
                else if (this.TryReadChar('b')) radix = 2;
                else if (this.TryReadChar('o')) radix = 8;
                else res.Append('0');
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.TryReadChar(chr => valid.Contains(chr) || chr == '_', out var c))
                if (c != '_')
                    res.Append(c);

            return new Token(TokenType.Number, Convert.ToUInt64(res.ToString(), radix).ToString());
        }

        private Token ReadSymbol() {
            if (this.TryReadChar(out var c)) {
                switch (c) {
                    case ';': return new Token(TokenType.Semicolon, c);
                    case '.': return new Token(TokenType.Period, c);
                    case ',': return new Token(TokenType.Comma, c);
                    case '(': return new Token(TokenType.OpenParenthesis, c);
                    case ')': return new Token(TokenType.CloseParenthesis, c);
                    case '{': return new Token(TokenType.OpenCurlyBrace, c);
                    case '}': return new Token(TokenType.CloseCurlyBrace, c);

                    case '+': return this.TryReadChar('=') ? new Token(TokenType.PlusEqual, c, '=') : new Token(TokenType.Plus, c);
                    case '-': return this.TryReadChar('=') ? new Token(TokenType.MinusEqual, c, '=') : new Token(TokenType.Minus, c);
                    case '*': return this.TryReadChar('=') ? new Token(TokenType.AsteriskEqual, c, '=') : new Token(TokenType.Asterisk, c);
                    case '/': return this.TryReadChar('=') ? new Token(TokenType.ForwardSlashEqual, c, '=') : new Token(TokenType.ForwardSlash, c);
                    case '^': return this.TryReadChar('=') ? new Token(TokenType.CaretEqual, c, '=') : new Token(TokenType.Caret, c);
                    case '%': return this.TryReadChar('=') ? new Token(TokenType.PercentEqual, c, '=') : new Token(TokenType.Percent, c);

                    case '&': return this.TryReadChar('=') ? new Token(TokenType.AmpersandEqual, c, '=') : new Token(TokenType.Ampersand, c);
                    case '|': return this.TryReadChar('=') ? new Token(TokenType.PipeEqual, c, '=') : new Token(TokenType.Pipe, c);
                    case '~': return this.TryReadChar('=') ? new Token(TokenType.TildeEqual, c, '=') : new Token(TokenType.Tilde, c);

                    case '=': return this.TryReadChar('=') ? new Token(TokenType.DoubleEqual, c, '=') : new Token(TokenType.Equal, c);

                    case '!':
                        if (this.TryReadChar('&')) return this.TryReadChar('=') ? new Token(TokenType.ExclamationPointAmpersandEqual, "!&=") : new Token(TokenType.ExclamationPointAmpersand, "!&");
                        else if (this.TryReadChar('|')) return this.TryReadChar('=') ? new Token(TokenType.ExclamationPointPipeEqual, "!|=") : new Token(TokenType.ExclamationPointPipe, "!|");
                        else if (this.TryReadChar('~')) return this.TryReadChar('=') ? new Token(TokenType.ExclamationPointTildeEqual, "!~=") : new Token(TokenType.ExclamationPointTilde, "!~");
                        else if (this.TryReadChar('=')) return new Token(TokenType.ExclamationPointEqual, "!=");
                        else return new Token(TokenType.ExclamationPoint, "=");

                    case '<':
                        if (this.TryReadChar('<')) {
                            if (this.TryReadChar('<')) {
                                if (this.TryReadChar('=')) {
                                    return new Token(TokenType.TripleLessThanEqual, "<<<=");
                                }
                                else {
                                    return new Token(TokenType.TripleLessThan, "<<<");
                                }
                            }
                            else {
                                if (this.TryReadChar('=')) {
                                    return new Token(TokenType.DoubleLessThanEqual, "<<=");
                                }
                                else {
                                    return new Token(TokenType.DoubleLessThan, "<<");
                                }
                            }
                        }
                        else if (this.TryReadChar('=')) {
                            return new Token(TokenType.LessThanEqual, "<=");
                        }
                        else {
                            return new Token(TokenType.LessThan, "<");
                        }

                    case '>':
                        if (this.TryReadChar('>')) {
                            if (this.TryReadChar('>')) {
                                if (this.TryReadChar('=')) {
                                    return new Token(TokenType.TripleGreaterThanEqual, ">>>=");
                                }
                                else {
                                    return new Token(TokenType.TripleGreaterThan, ">>>");
                                }
                            }
                            else {
                                if (this.TryReadChar('=')) {
                                    return new Token(TokenType.DoubleGreaterThanEqual, ">>=");
                                }
                                else {
                                    return new Token(TokenType.DoubleGreaterThan, ">>");
                                }
                            }
                        }
                        else if (this.TryReadChar('=')) {
                            return new Token(TokenType.GreaterThanEqual, ">=");
                        }
                        else {
                            return new Token(TokenType.GreaterThan, ">");
                        }
                }

                throw new UnexpectedCharacterException(this.CurrentPosition, c);
            }

            throw new InvalidOperationException();
        }

        private Token LexNextToken() {
            if (!this.TryPeekChar(out var c)) {
                this.eof = true;

                return default(Token);
            }

            if (char.IsLetter(c)) {
                return this.ReadWord();
            }
            else if (char.IsNumber(c)) {
                return this.ReadNumber();
            }
            else if (char.IsWhiteSpace(c)) {
                _ = this.ReadWhitespace();

                return this.LexNextToken();
            }
            else {
                return this.ReadSymbol();
            }
        }

        public List<Token> ToList() {
            var res = new List<Token>();

            while (this.TryRead(out var t))
                res.Add(t);

            return res;
        }

        public bool TryPeek(out Token token) {
            token = this.currentToken;

            return !this.eof;
        }

        public bool TryRead(out Token token) {
            token = this.currentToken;

            var res = !this.eof;

            this.currentToken = this.LexNextToken();

            return res;
        }

        public Token Read(TokenType type) => this.TryRead(type, out var token) ? token : throw new ExpectedTokenException(this.CurrentPosition, type);
        public bool TryRead(TokenType type) => this.TryRead(type, out _);
        public bool TryRead(TokenType type, out Token token) => this.TryRead(t => t.Type == type, out token);
        public bool TryRead(Predicate<Token> predicate, out Token token) => this.TryPeek(predicate, out token) && this.TryRead(out _);

        public Token Peek(TokenType type) => this.TryPeek(type, out var token) ? token : throw new ExpectedTokenException(this.CurrentPosition, type);
        public bool TryPeek(TokenType type) => this.TryPeek(type, out _);
        public bool TryPeek(TokenType type, out Token token) => this.TryPeek(t => t.Type == type, out token);
        public bool TryPeek(Predicate<Token> predicate, out Token token) => this.TryPeek(out token) && predicate(token);
    }
}
