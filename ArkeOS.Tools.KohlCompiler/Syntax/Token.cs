﻿using System;
using System.Diagnostics;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public enum TokenType {
        IntegerLiteral,
        FloatLiteral,
        BoolLiteral,
        NullLiteral,

        Identifier,

        Whitespace,

        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        StructKeyword,
        FuncKeyword,
        VarKeyword,
        ConstKeyword,
        ReturnKeyword,

        DbgKeyword,
        BrkKeyword,
        HltKeyword,
        NopKeyword,
        IntKeyword,
        EintKeyword,
        InteKeyword,
        IntdKeyword,
        XchgKeyword,
        CasKeyword,
        CpyKeyword,

        Semicolon,
        Comma,
        Period,
        Colon,
        MinusGreaterThan,

        OpenParenthesis,
        CloseParenthesis,

        OpenCurlyBrace,
        CloseCurlyBrace,

        OpenSquareBrace,
        CloseSquareBrace,

        Equal,
        DoubleEqual,

        ExclamationPoint,
        ExclamationPointEqual,

        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Caret,
        Percent,
        Ampersand,
        Pipe,
        Tilde,
        ExclamationPointAmpersand,
        ExclamationPointPipe,
        ExclamationPointTilde,
        LessThan,
        DoubleLessThan,
        TripleLessThan,
        GreaterThan,
        DoubleGreaterThan,
        TripleGreaterThan,

        PlusEqual,
        MinusEqual,
        AsteriskEqual,
        ForwardSlashEqual,
        CaretEqual,
        PercentEqual,
        AmpersandEqual,
        PipeEqual,
        TildeEqual,
        ExclamationPointAmpersandEqual,
        ExclamationPointPipeEqual,
        ExclamationPointTildeEqual,
        LessThanEqual,
        DoubleLessThanEqual,
        TripleLessThanEqual,
        GreaterThanEqual,
        DoubleGreaterThanEqual,
        TripleGreaterThanEqual,
    }

    public enum TokenClass {
        Operator,
        Assignment,
        Literal,
        Identifier,
        BlockKeyword,
        StatementKeyword,
        IntrinsicKeyword,
        Brace,
        Separator,
        Whitespace,
    }

    public struct Token {
        public PositionInfo Position;
        public TokenType Type;
        public TokenClass Class;
        public string Value;

        public Token(PositionInfo position, TokenType type) : this(position, type, string.Empty) { }
        public Token(PositionInfo position, TokenType type, string value) => (this.Position, this.Type, this.Value, this.Class) = (position, type, value, Token.GetClass(type));

        public override string ToString() => !string.IsNullOrEmpty(this.Value) ? $"{this.Type}<{this.Value}>" : this.Type.ToString();

        private static TokenClass GetClass(TokenType token) {
            switch (token) {
                case TokenType.OpenParenthesis:
                case TokenType.CloseParenthesis:

                case TokenType.DoubleEqual:
                case TokenType.ExclamationPoint:
                case TokenType.ExclamationPointEqual:

                case TokenType.LessThanEqual:
                case TokenType.GreaterThanEqual:

                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Caret:
                case TokenType.Percent:
                case TokenType.Ampersand:
                case TokenType.Pipe:
                case TokenType.Tilde:

                case TokenType.ExclamationPointAmpersand:
                case TokenType.ExclamationPointPipe:
                case TokenType.ExclamationPointTilde:
                case TokenType.LessThan:
                case TokenType.DoubleLessThan:
                case TokenType.TripleLessThan:
                case TokenType.GreaterThan:
                case TokenType.DoubleGreaterThan:
                case TokenType.TripleGreaterThan:
                    return TokenClass.Operator;

                case TokenType.Equal:

                case TokenType.PlusEqual:
                case TokenType.MinusEqual:
                case TokenType.AsteriskEqual:
                case TokenType.ForwardSlashEqual:
                case TokenType.CaretEqual:
                case TokenType.PercentEqual:
                case TokenType.AmpersandEqual:
                case TokenType.PipeEqual:
                case TokenType.TildeEqual:

                case TokenType.ExclamationPointAmpersandEqual:
                case TokenType.ExclamationPointPipeEqual:
                case TokenType.ExclamationPointTildeEqual:
                case TokenType.DoubleLessThanEqual:
                case TokenType.TripleLessThanEqual:
                case TokenType.DoubleGreaterThanEqual:
                case TokenType.TripleGreaterThanEqual:
                    return TokenClass.Assignment;

                case TokenType.IntegerLiteral:
                case TokenType.FloatLiteral:
                case TokenType.BoolLiteral:
                case TokenType.NullLiteral:
                    return TokenClass.Literal;

                case TokenType.Identifier:
                    return TokenClass.Identifier;

                case TokenType.IfKeyword:
                case TokenType.ElseKeyword:
                case TokenType.WhileKeyword:
                case TokenType.StructKeyword:
                case TokenType.FuncKeyword:
                    return TokenClass.BlockKeyword;

                case TokenType.VarKeyword:
                case TokenType.ConstKeyword:
                case TokenType.ReturnKeyword:
                    return TokenClass.StatementKeyword;

                case TokenType.DbgKeyword:
                case TokenType.BrkKeyword:
                case TokenType.HltKeyword:
                case TokenType.NopKeyword:
                case TokenType.IntKeyword:
                case TokenType.EintKeyword:
                case TokenType.InteKeyword:
                case TokenType.IntdKeyword:
                case TokenType.XchgKeyword:
                case TokenType.CasKeyword:
                case TokenType.CpyKeyword:
                    return TokenClass.IntrinsicKeyword;

                case TokenType.Semicolon:
                case TokenType.Comma:
                case TokenType.Period:
                case TokenType.Colon:
                case TokenType.MinusGreaterThan:
                    return TokenClass.Separator;

                case TokenType.OpenCurlyBrace:
                case TokenType.CloseCurlyBrace:
                case TokenType.OpenSquareBrace:
                case TokenType.CloseSquareBrace:
                    return TokenClass.Brace;

                case TokenType.Whitespace:
                    return TokenClass.Whitespace;

                default:
                    Debug.Assert(false);

                    throw new InvalidOperationException();
            }
        }
    }
}
