﻿using ArkeOS.Tools.KohlCompiler.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private readonly TokenStream stream;
        private readonly IReadOnlyList<string> files;
        private readonly IReadOnlyList<string> fileContents;
        private readonly int[] fileLengths;
        private string currentFileContents;
        private int currentFileLength;
        private int fileIndex;
        private int contentsIndex;
        private Token currentToken;

        public Lexer(IReadOnlyList<string> files) {
            this.stream = new TokenStream(this);
            this.files = files;
            this.fileContents = this.files.Select(f => File.ReadAllText(f)).ToList();
            this.fileLengths = this.fileContents.Select(s => s.Length).ToArray();
            this.currentFileContents = this.fileContents[0];
            this.currentFileLength = this.fileLengths[0];
            this.fileIndex = 0;
            this.contentsIndex = 0;

            this.LexNextToken();
        }

        private void Advance() {
            if (++this.contentsIndex >= this.currentFileLength) {
                this.contentsIndex = 0;

                if (++this.fileIndex < this.fileContents.Count) {
                    this.currentFileContents = this.fileContents[this.fileIndex];
                    this.currentFileLength = this.fileLengths[this.fileIndex];
                }
                else {
                    this.currentFileContents = string.Empty;
                    this.currentFileLength = 0;
                }
            }
        }

        private bool Peek(out char value) {
            if (this.contentsIndex < this.currentFileLength) {
                value = this.currentFileContents[this.contentsIndex];

                return true;
            }
            else {
                value = default(char);

                return false;
            }
        }

        private void ThrowUnexpectedCharacterAtCurrent(char c) {
            var lengths = this.currentFileContents.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => l.Length).ToArray();
            var line = 0;
            var idx = 0;

            while (idx + lengths[line] < this.contentsIndex)
                idx += lengths[line++] + Environment.NewLine.Length;

            throw new UnexpectedCharacterException(this.files[this.fileIndex], line + 1, this.contentsIndex - idx + 1, c);
        }

        private Token ReadIdentifier() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && (char.IsLetterOrDigit(c) || c == '_')) {
                this.Advance();

                res.Append(c);
            }

            return new Token(TokenType.Identifier, res.ToString());
        }

        private Token ReadNumber() {
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
                    res.Append(s);
                }
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.Peek(out var c) && (valid.Contains(c) || c == '_')) {
                this.Advance();

                if (c != '_')
                    res.Append(c);
            }

            return new Token(TokenType.Number, Convert.ToUInt64(res.ToString(), radix).ToString());
        }

        private Token ReadWhitespace() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && char.IsWhiteSpace(c)) {
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
                default: this.ThrowUnexpectedCharacterAtCurrent(c); break;
            }

            this.Advance();

            return res;
        }

        private void LexNextToken() {
            if (!this.Peek(out var c)) {
                this.currentToken = default(Token);

                return;
            }

            if (char.IsLetter(c)) {
                this.currentToken = this.ReadIdentifier();
            }
            else if (char.IsNumber(c)) {
                this.currentToken = this.ReadNumber();
            }
            else if (char.IsWhiteSpace(c)) {
                this.currentToken = this.ReadWhitespace();

                this.LexNextToken();
            }
            else {
                this.currentToken = this.ReadSymbol(c);
            }
        }

        public bool ReadNext(out Token token) {
            var res = this.PeekNext(out token);

            if (res)
                this.LexNextToken();

            return res;
        }

        public bool PeekNext(out Token token) {
            token = this.currentToken;

            return this.contentsIndex < this.currentFileLength;
        }

        public TokenStream GetStream() => this.stream;
    }
}
