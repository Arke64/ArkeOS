using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private static IReadOnlyDictionary<string, TokenType> Keywords { get; } = new Dictionary<string, TokenType> {
            ["if"] = TokenType.IfKeyword,
            ["else"] = TokenType.ElseKeyword,
            ["while"] = TokenType.WhileKeyword,
            ["func"] = TokenType.FuncKeyword,
            ["var"] = TokenType.VarKeyword,
            ["const"] = TokenType.ConstKeyword,
            ["return"] = TokenType.ReturnKeyword,

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

        private static IReadOnlyDictionary<string, TokenType> Literals { get; } = new Dictionary<string, TokenType> {
            ["true"] = TokenType.BoolLiteral,
            ["false"] = TokenType.BoolLiteral,
            ["null"] = TokenType.NullLiteral,
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
        private readonly StringBuilder builder;
        private FileInfo file;
        private bool eof;
        private Token currentToken;

        public PositionInfo CurrentPosition => this.file != null ? new PositionInfo(this.file.Path, this.file.Line, this.file.Column) : default(PositionInfo);

        public Lexer(CompilationOptions options) {
            this.files = new Queue<FileInfo>(options.Sources.Select(f => new FileInfo(f)));
            this.builder = new StringBuilder();
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

        private Token ReadWhitespace(PositionInfo start) {
            this.builder.Clear();

            while (this.TryReadChar(char.IsWhiteSpace, out var c))
                this.builder.Append(c);

            return new Token(start, TokenType.Whitespace, this.builder.ToString());
        }

        private Token ReadWord(PositionInfo start) {
            this.builder.Clear();

            while (this.TryReadChar(chr => char.IsLetterOrDigit(chr) || chr == '_', out var c))
                this.builder.Append(c);

            var str = this.builder.ToString();

            if (Lexer.Keywords.TryGetValue(str, out var t1)) return new Token(start, t1);
            if (Lexer.Literals.TryGetValue(str, out var t2)) return new Token(start, t2, str);

            return new Token(start, TokenType.Identifier, str);
        }

        private Token ReadNumber(PositionInfo start) {
            this.builder.Clear();

            var radix = 10;

            if (this.TryReadChar('0')) {
                if (this.TryReadChar('d')) radix = 10;
                else if (this.TryReadChar('x')) radix = 16;
                else if (this.TryReadChar('b')) radix = 2;
                else if (this.TryReadChar('o')) radix = 8;
                else this.builder.Append('0');
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.TryReadChar(chr => valid.Contains(chr) || chr == '_', out var c))
                if (c != '_')
                    this.builder.Append(c);

            return new Token(start, TokenType.IntegerLiteral, Convert.ToUInt64(this.builder.ToString(), radix).ToString());
        }

        private Token ReadSymbol(PositionInfo start) {
            var tok = default(TokenType);

            this.TryReadChar(out var c);

            switch (c) {
                case ';': tok = TokenType.Semicolon; break;
                case '.': tok = TokenType.Period; break;
                case ':': tok = TokenType.Colon; break;
                case ',': tok = TokenType.Comma; break;
                case '(': tok = TokenType.OpenParenthesis; break;
                case ')': tok = TokenType.CloseParenthesis; break;
                case '{': tok = TokenType.OpenCurlyBrace; break;
                case '}': tok = TokenType.CloseCurlyBrace; break;
                case '[': tok = TokenType.OpenSquareBrace; break;
                case ']': tok = TokenType.CloseSquareBrace; break;

                case '+': tok = this.TryReadChar('=') ? TokenType.PlusEqual : TokenType.Plus; break;
                case '-': tok = this.TryReadChar('=') ? TokenType.MinusEqual : TokenType.Minus; break;
                case '*': tok = this.TryReadChar('=') ? TokenType.AsteriskEqual : TokenType.Asterisk; break;
                case '/': tok = this.TryReadChar('=') ? TokenType.ForwardSlashEqual : TokenType.ForwardSlash; break;
                case '^': tok = this.TryReadChar('=') ? TokenType.CaretEqual : TokenType.Caret; break;
                case '%': tok = this.TryReadChar('=') ? TokenType.PercentEqual : TokenType.Percent; break;
                case '&': tok = this.TryReadChar('=') ? TokenType.AmpersandEqual : TokenType.Ampersand; break;
                case '|': tok = this.TryReadChar('=') ? TokenType.PipeEqual : TokenType.Pipe; break;
                case '~': tok = this.TryReadChar('=') ? TokenType.TildeEqual : TokenType.Tilde; break;
                case '=': tok = this.TryReadChar('=') ? TokenType.DoubleEqual : TokenType.Equal; break;

                case '!':
                    if (this.TryReadChar('&')) tok = this.TryReadChar('=') ? TokenType.ExclamationPointAmpersandEqual : TokenType.ExclamationPointAmpersand;
                    else if (this.TryReadChar('|')) tok = this.TryReadChar('=') ? TokenType.ExclamationPointPipeEqual : TokenType.ExclamationPointPipe;
                    else if (this.TryReadChar('~')) tok = this.TryReadChar('=') ? TokenType.ExclamationPointTildeEqual : TokenType.ExclamationPointTilde;
                    else tok = this.TryReadChar('=') ? TokenType.ExclamationPointEqual : TokenType.ExclamationPoint;
                    break;

                case '<':
                    tok = this.TryReadChar('<') ? (this.TryReadChar('<') ?
                        (this.TryReadChar('=') ? TokenType.TripleLessThanEqual : TokenType.TripleLessThan) :
                        (this.TryReadChar('=') ? TokenType.DoubleLessThanEqual : TokenType.DoubleLessThan)) :
                        (this.TryReadChar('=') ? TokenType.LessThanEqual : TokenType.LessThan);
                    break;

                case '>':
                    tok = this.TryReadChar('>') ? (this.TryReadChar('>') ?
                        (this.TryReadChar('=') ? TokenType.TripleGreaterThanEqual : TokenType.TripleGreaterThan) :
                        (this.TryReadChar('=') ? TokenType.DoubleGreaterThanEqual : TokenType.DoubleGreaterThan)) :
                        (this.TryReadChar('=') ? TokenType.GreaterThanEqual : TokenType.GreaterThan);
                    break;

                default: throw new UnexpectedException(this.CurrentPosition, c);
            }

            return new Token(start, tok);
        }

        private Token LexNextToken() {
            if (!this.TryPeekChar(out var c)) {
                this.eof = true;

                return default(Token);
            }

            if (char.IsLetter(c)) return this.ReadWord(this.CurrentPosition);
            if (char.IsNumber(c)) return this.ReadNumber(this.CurrentPosition);
            if (char.IsWhiteSpace(c)) { _ = this.ReadWhitespace(this.CurrentPosition); return this.LexNextToken(); }
            return this.ReadSymbol(this.CurrentPosition);
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

        public Token Read() => this.TryRead(out var token) ? token : throw new ExpectedException(this.CurrentPosition, "token");
        public Token Read(TokenType type) => this.TryRead(out var token) && token.Type == type ? token : throw new ExpectedException(this.CurrentPosition, type);
        public Token Read(TokenClass cls) => this.TryRead(out var token) && token.Class == cls ? token : throw new ExpectedException(this.CurrentPosition, cls);
        public bool TryRead(TokenType type) => this.TryPeek(type) && this.TryRead(out _);
        public bool TryRead(TokenClass cls) => this.TryPeek(cls) && this.TryRead(out _);

        public Token Peek() => this.TryPeek(out var token) ? token : throw new ExpectedException(this.CurrentPosition, "token");
        public Token Peek(TokenType type) => this.TryPeek(out var token) && token.Type == type ? token : throw new ExpectedException(this.CurrentPosition, type);
        public Token Peek(TokenClass cls) => this.TryPeek(out var token) && token.Class == cls ? token : throw new ExpectedException(this.CurrentPosition, cls);
        public bool TryPeek(TokenType type) => this.TryPeek(out var token) && token.Type == type;
        public bool TryPeek(TokenClass cls) => this.TryPeek(out var token) && token.Class == cls;
    }
}
