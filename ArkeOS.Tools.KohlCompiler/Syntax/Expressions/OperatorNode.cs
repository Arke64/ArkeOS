using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public enum Operator {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Remainder,
        Exponentiation,
        UnaryMinus,
        ShiftLeft,
        ShiftRight,
        RotateLeft,
        RotateRight,
        And,
        Or,
        Xor,
        NotAnd,
        NotOr,
        NotXor,
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Not,
        AddressOf,
        Dereference,
        OpenParenthesis,
        CloseParenthesis,
    }

    public enum OperatorClass {
        Unary,
        Binary,
    }

    public sealed class OperatorNode : SyntaxNode {
        private static IReadOnlyDictionary<Operator, (int, bool)> Defs { get; } = new Dictionary<Operator, (int, bool)> {
            [Operator.OpenParenthesis] = (0, true),
            [Operator.CloseParenthesis] = (0, true),

            [Operator.Or] = (10, true),
            [Operator.NotOr] = (10, true),

            [Operator.Xor] = (20, true),
            [Operator.NotXor] = (20, true),

            [Operator.And] = (30, true),
            [Operator.NotAnd] = (30, true),

            [Operator.Equals] = (40, true),
            [Operator.NotEquals] = (40, true),

            [Operator.LessThan] = (50, true),
            [Operator.LessThanOrEqual] = (50, true),
            [Operator.GreaterThan] = (50, true),
            [Operator.GreaterThanOrEqual] = (50, true),

            [Operator.ShiftLeft] = (60, true),
            [Operator.ShiftRight] = (60, true),
            [Operator.RotateLeft] = (60, true),
            [Operator.RotateRight] = (60, true),

            [Operator.Addition] = (70, true),
            [Operator.Subtraction] = (70, true),

            [Operator.Multiplication] = (80, true),
            [Operator.Division] = (80, true),
            [Operator.Remainder] = (80, true),

            [Operator.Exponentiation] = (90, false),

            [Operator.UnaryMinus] = (100, false),
            [Operator.Not] = (100, false),
            [Operator.AddressOf] = (100, false),
            [Operator.Dereference] = (100, false),
        };

        public Operator Operator { get; }
        public OperatorClass Class { get; }
        public int Precedence { get; }
        public bool IsLeftAssociative { get; }

        private OperatorNode(Operator op, OperatorClass cls) => (this.Operator, this.Class, (this.Precedence, this.IsLeftAssociative)) = (op, cls, OperatorNode.Defs[op]);

        public static OperatorNode FromOperator(Operator op) {
            var cls = OperatorClass.Binary;

            switch (op) {
                case Operator.UnaryMinus:
                case Operator.Not:
                case Operator.AddressOf:
                case Operator.Dereference:
                    cls = OperatorClass.Binary;

                    break;
            }

            return new OperatorNode(op, cls);
        }

        public static OperatorNode FromToken(Token token, OperatorClass cls) {
            var op = default(Operator);

            if (cls == OperatorClass.Binary) {
                switch (token.Type) {
                    case TokenType.Plus: op = Operator.Addition; break;
                    case TokenType.Minus: op = Operator.Subtraction; break;
                    case TokenType.Asterisk: op = Operator.Multiplication; break;
                    case TokenType.ForwardSlash: op = Operator.Division; break;
                    case TokenType.Percent: op = Operator.Remainder; break;
                    case TokenType.Caret: op = Operator.Exponentiation; break;
                    case TokenType.DoubleLessThan: op = Operator.ShiftLeft; break;
                    case TokenType.DoubleGreaterThan: op = Operator.ShiftRight; break;
                    case TokenType.TripleLessThan: op = Operator.RotateLeft; break;
                    case TokenType.TripleGreaterThan: op = Operator.RotateRight; break;
                    case TokenType.Ampersand: op = Operator.And; break;
                    case TokenType.Pipe: op = Operator.Or; break;
                    case TokenType.Tilde: op = Operator.Xor; break;
                    case TokenType.ExclamationPointAmpersand: op = Operator.NotAnd; break;
                    case TokenType.ExclamationPointPipe: op = Operator.NotOr; break;
                    case TokenType.ExclamationPointTilde: op = Operator.NotXor; break;
                    case TokenType.DoubleEqual: op = Operator.Equals; break;
                    case TokenType.ExclamationPointEqual: op = Operator.NotEquals; break;
                    case TokenType.LessThan: op = Operator.LessThan; break;
                    case TokenType.LessThanEqual: op = Operator.LessThanOrEqual; break;
                    case TokenType.GreaterThan: op = Operator.GreaterThan; break;
                    case TokenType.GreaterThanEqual: op = Operator.GreaterThanOrEqual; break;
                    case TokenType.OpenParenthesis: op = Operator.OpenParenthesis; break;
                    case TokenType.CloseParenthesis: op = Operator.CloseParenthesis; break;
                    default: return null;
                }
            }
            else {
                switch (token.Type) {
                    case TokenType.Minus: op = Operator.UnaryMinus; break;
                    case TokenType.ExclamationPoint: op = Operator.Not; break;
                    case TokenType.Ampersand: op = Operator.AddressOf; break;
                    case TokenType.Asterisk: op = Operator.Dereference; break;
                    default: return null;
                }
            }

            return new OperatorNode(op, cls);
        }

        public static OperatorNode FromCompoundToken(Token token) {
            var op = default(Operator);

            switch (token.Type) {
                case TokenType.PlusEqual: op = Operator.Addition; break;
                case TokenType.MinusEqual: op = Operator.Subtraction; break;
                case TokenType.AsteriskEqual: op = Operator.Multiplication; break;
                case TokenType.ForwardSlashEqual: op = Operator.Division; break;
                case TokenType.PercentEqual: op = Operator.Remainder; break;
                case TokenType.CaretEqual: op = Operator.Exponentiation; break;
                case TokenType.DoubleLessThanEqual: op = Operator.ShiftLeft; break;
                case TokenType.DoubleGreaterThanEqual: op = Operator.ShiftRight; break;
                case TokenType.TripleLessThanEqual: op = Operator.RotateLeft; break;
                case TokenType.TripleGreaterThanEqual: op = Operator.RotateRight; break;
                case TokenType.AmpersandEqual: op = Operator.And; break;
                case TokenType.PipeEqual: op = Operator.Or; break;
                case TokenType.TildeEqual: op = Operator.Xor; break;
                case TokenType.ExclamationPointAmpersandEqual: op = Operator.NotAnd; break;
                case TokenType.ExclamationPointPipeEqual: op = Operator.NotOr; break;
                case TokenType.ExclamationPointTildeEqual: op = Operator.NotXor; break;
                default: return null;
            }

            return new OperatorNode(op, OperatorClass.Binary);
        }
    }
}
