﻿using ArkeOS.Tools.KohlCompiler.Exceptions;
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

        private bool TryPeekChar(out char chr) {
            if (this.file != null) {
                chr = this.file.Current;
                return true;
            }
            else {
                chr = default(char);
                return false;
            }
        }

        private bool TryReadChar(out char chr) {
            var res = this.TryPeekChar(out chr);

            if (res && this.file.Advance())
                this.file = this.files.Any() ? this.files.Dequeue() : null;

            return res;
        }

        private bool TryReadChar(char chr) => this.TryReadChar(c => c == chr, out _);
        private bool TryReadChar(Predicate<char> predicate, out char chr) => this.TryPeekChar(out chr) && predicate(chr) && this.TryReadChar(out _);

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

            return Lexer.Keywords.TryGetValue(str, out var t) ? new Token(t) : new Token(TokenType.Identifier, str);
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
            this.TryReadChar(out var c);

            switch (c) {
                case ';': return new Token(TokenType.Semicolon);
                case '.': return new Token(TokenType.Period);
                case ',': return new Token(TokenType.Comma);
                case '(': return new Token(TokenType.OpenParenthesis);
                case ')': return new Token(TokenType.CloseParenthesis);
                case '{': return new Token(TokenType.OpenCurlyBrace);
                case '}': return new Token(TokenType.CloseCurlyBrace);

                case '+': return new Token(this.TryReadChar('=') ? TokenType.PlusEqual : TokenType.Plus);
                case '-': return new Token(this.TryReadChar('=') ? TokenType.MinusEqual : TokenType.Minus);
                case '*': return new Token(this.TryReadChar('=') ? TokenType.AsteriskEqual : TokenType.Asterisk);
                case '/': return new Token(this.TryReadChar('=') ? TokenType.ForwardSlashEqual : TokenType.ForwardSlash);
                case '^': return new Token(this.TryReadChar('=') ? TokenType.CaretEqual : TokenType.Caret);
                case '%': return new Token(this.TryReadChar('=') ? TokenType.PercentEqual : TokenType.Percent);
                case '&': return new Token(this.TryReadChar('=') ? TokenType.AmpersandEqual : TokenType.Ampersand);
                case '|': return new Token(this.TryReadChar('=') ? TokenType.PipeEqual : TokenType.Pipe);
                case '~': return new Token(this.TryReadChar('=') ? TokenType.TildeEqual : TokenType.Tilde);
                case '=': return new Token(this.TryReadChar('=') ? TokenType.DoubleEqual : TokenType.Equal);

                case '!':
                    if (this.TryReadChar('&')) return new Token(this.TryReadChar('=') ? TokenType.ExclamationPointAmpersandEqual : TokenType.ExclamationPointAmpersand);
                    if (this.TryReadChar('|')) return new Token(this.TryReadChar('=') ? TokenType.ExclamationPointPipeEqual : TokenType.ExclamationPointPipe);
                    if (this.TryReadChar('~')) return new Token(this.TryReadChar('=') ? TokenType.ExclamationPointTildeEqual : TokenType.ExclamationPointTilde);
                    return new Token(this.TryReadChar('=') ? TokenType.ExclamationPointEqual : TokenType.ExclamationPoint);

                case '<':
                    return this.TryReadChar('<') ? (this.TryReadChar('<') ?
                  new Token(this.TryReadChar('=') ? TokenType.TripleLessThanEqual : TokenType.TripleLessThan) :
                  new Token(this.TryReadChar('=') ? TokenType.DoubleLessThanEqual : TokenType.DoubleLessThan)) :
                  new Token(this.TryReadChar('=') ? TokenType.LessThanEqual : TokenType.LessThan);

                case '>':
                    return this.TryReadChar('>') ? (this.TryReadChar('>') ?
                  new Token(this.TryReadChar('=') ? TokenType.TripleGreaterThanEqual : TokenType.TripleGreaterThan) :
                  new Token(this.TryReadChar('=') ? TokenType.DoubleGreaterThanEqual : TokenType.DoubleGreaterThan)) :
                  new Token(this.TryReadChar('=') ? TokenType.GreaterThanEqual : TokenType.GreaterThan);

                default: throw new UnexpectedCharacterException(this.CurrentPosition, c);
            }
        }

        private Token LexNextToken() {
            if (!this.TryPeekChar(out var c)) {
                this.eof = true;

                return default(Token);
            }

            if (char.IsLetter(c)) return this.ReadWord();
            if (char.IsNumber(c)) return this.ReadNumber();
            if (char.IsWhiteSpace(c)) { _ = this.ReadWhitespace(); return this.LexNextToken(); }
            return this.ReadSymbol();
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
