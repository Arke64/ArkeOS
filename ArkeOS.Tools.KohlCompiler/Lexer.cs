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

            var str = res.ToString();

            return new Token(Lexer.Keywords.TryGetValue(str, out var t) ? t : TokenType.Identifier, str);
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
                case ';': res = new Token(TokenType.Semicolon, c); break;
                case '.': res = new Token(TokenType.Period, c); break;
                case ',': res = new Token(TokenType.Comma, c); break;
                case '(': res = new Token(TokenType.OpenParenthesis, c); break;
                case ')': res = new Token(TokenType.CloseParenthesis, c); break;
                case '{': res = new Token(TokenType.OpenCurlyBrace, c); break;
                case '}': res = new Token(TokenType.CloseCurlyBrace, c); break;

                case '+': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.PlusEqual, c, cc); } else { return new Token(TokenType.Plus, c); } } break;
                case '-': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.MinusEqual, c, cc); } else { return new Token(TokenType.Minus, c); } } break;
                case '*': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.AsteriskEqual, c, cc); } else { return new Token(TokenType.Asterisk, c); } } break;
                case '/': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.ForwardSlashEqual, c, cc); } else { return new Token(TokenType.ForwardSlash, c); } } break;
                case '^': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.CaretEqual, c, cc); } else { return new Token(TokenType.Caret, c); } } break;
                case '%': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.PercentEqual, c, cc); } else { return new Token(TokenType.Percent, c); } } break;

                case '&': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.AmpersandEqual, c, cc); } else { return new Token(TokenType.Ampersand, c); } } break;
                case '|': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.PipeEqual, c, cc); } else { return new Token(TokenType.Pipe, c); } } break;
                case '~': { this.Advance(); if (this.PeekChar(out var cc) && cc == '=') { res = new Token(TokenType.TildeEqual, c, cc); } else { return new Token(TokenType.Tilde, c); } } break;

                case '=': {
                        this.Advance();

                        if (this.PeekChar(out var cc) && cc == '=') {
                            res = new Token(TokenType.DoubleEqual, c, cc);
                        }
                        else {
                            return new Token(TokenType.Equal, c);
                        }
                    }

                    break;

                case '!': {
                        this.Advance();

                        if (this.PeekChar(out var cc)) {
                            switch (cc) {
                                case '&': { if (this.PeekChar(out var ccc) && ccc == '=') { this.Advance(); res = new Token(TokenType.ExclamationPointAmpersandEqual, c, cc, ccc); } else { res = new Token(TokenType.ExclamationPointAmpersand, c, cc); } } break;
                                case '|': { if (this.PeekChar(out var ccc) && ccc == '=') { this.Advance(); res = new Token(TokenType.ExclamationPointPipeEqual, c, cc, ccc); } else { res = new Token(TokenType.ExclamationPointPipe, c, cc); } } break;
                                case '~': { if (this.PeekChar(out var ccc) && ccc == '=') { this.Advance(); res = new Token(TokenType.ExclamationPointTildeEqual, c, cc, ccc); } else { res = new Token(TokenType.ExclamationPointTilde, c, cc); } } break;
                                case '=': res = new Token(TokenType.ExclamationPointEqual, c, cc); break;
                                default: return new Token(TokenType.ExclamationPoint, c);
                            }
                        }
                        else {
                            return new Token(TokenType.ExclamationPoint, c);
                        }
                    }

                    break;

                case '<': {
                        this.Advance();

                        if (this.PeekChar(out var cc)) {
                            switch (cc) {
                                case '<':
                                    this.Advance();

                                    if (this.PeekChar(out var ccc) && ccc == '<') {
                                        this.Advance();

                                        if (this.PeekChar(out var cccc) && cccc == '=') {
                                            this.Advance();

                                            res = new Token(TokenType.TripleLessThanEqual, c, cc, ccc, cccc);
                                        }
                                        else {
                                            return new Token(TokenType.TripleLessThan, c, cc);
                                        }
                                    }
                                    else {
                                        if (this.PeekChar(out var cccc) && cccc == '=') {
                                            this.Advance();

                                            res = new Token(TokenType.DoubleLessThanEqual, c, cc, ccc, cccc);
                                        }
                                        else {
                                            return new Token(TokenType.DoubleLessThan, c, cc);
                                        }
                                    }

                                    break;

                                case '=': res = new Token(TokenType.LessThanEqual, c, cc); break;
                                default: return new Token(TokenType.LessThan, c);
                            }
                        }
                        else {
                            return new Token(TokenType.LessThan, c);
                        }
                    }

                    break;

                case '>': {
                        this.Advance();

                        if (this.PeekChar(out var cc)) {
                            switch (cc) {
                                case '>':
                                    this.Advance();

                                    if (this.PeekChar(out var ccc) && ccc == '>') {
                                        this.Advance();

                                        if (this.PeekChar(out var cccc) && cccc == '=') {
                                            this.Advance();

                                            res = new Token(TokenType.TripleGreaterThanEqual, c, cc, ccc, cccc);
                                        }
                                        else {
                                            return new Token(TokenType.TripleGreaterThan, c, cc);
                                        }
                                    }
                                    else {
                                        if (this.PeekChar(out var cccc) && cccc == '=') {
                                            this.Advance();

                                            res = new Token(TokenType.DoubleGreaterThanEqual, c, cc, ccc, cccc);
                                        }
                                        else {
                                            return new Token(TokenType.DoubleGreaterThan, c, cc);
                                        }
                                    }

                                    break;

                                case '=': res = new Token(TokenType.GreaterThanEqual, c, cc); break;
                                default: return new Token(TokenType.GreaterThan, c);
                            }
                        }
                        else {
                            return new Token(TokenType.GreaterThan, c);
                        }
                    }

                    break;

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

        public bool Peek(TokenType type) => this.Peek(type, out _);
        public bool Peek(TokenType type, out Token token) => this.Peek(t => t.Type == type, out token);
        public bool Peek(Func<Token, bool> validator, out Token token) => this.Peek(out token) && validator(token);

        public bool Read(TokenType type) => this.Read(type, out _);
        public bool Read(TokenType type, out Token token) => this.Read(t => t.Type == type, out token);
        public bool Read(Func<Token, bool> validator, out Token token) => this.Peek(out token) && validator(token) && this.Read(out _);
    }
}
