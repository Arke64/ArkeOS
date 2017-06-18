﻿using System;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler {
    public class TokenStream {
        private readonly Lexer lexer;

        public TokenStream(Lexer lexer) => this.lexer = lexer;

        public bool Peek(out Token value) => this.lexer.PeekNext(out value);
        public bool Read(out Token value) { bool res = this.lexer.ReadNext(out value); if (res) this.CurrentPosition = value.Position; return res; }

        public bool Peek(Func<Token, bool> validator, out Token value) => this.Peek(out value) && validator(value);
        public bool Read(Func<Token, bool> validator, out Token value) => this.Peek(out value) && validator(value) && this.Read(out _);

        public bool Peek(TokenType type, out Token value) => this.Peek(t => t.Type == type, out value);
        public bool Read(TokenType type, out Token value) => this.Read(t => t.Type == type, out value);

        public bool Peek(TokenType type) => this.Peek(type, out _);
        public bool Read(TokenType type) => this.Read(type, out _);

        public bool AtEnd => this.lexer.AtEnd;
        public PositionInfo CurrentPosition { get; private set; }

        public List<Token> ToList() {
            var res = new List<Token>();

            while (this.Read(out var t))
                res.Add(t);

            return res;
        }
    }
}