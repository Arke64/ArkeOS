using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.Nodes {
    public enum Operator {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Remainder,
        Exponentiation,
        UnaryPlus,
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
        OpenParenthesis,
        CloseParenthesis,
    }

    public enum OperatorClass {
        Unary,
        Binary,
    }

    public sealed class OperatorNode : Node {
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
            [Operator.UnaryPlus] = (100, false),
            [Operator.Not] = (100, false),
        };

        public Operator Operator { get; }
        public int Precedence { get; }
        public bool IsLeftAssociative { get; }

        public OperatorNode(Operator op) => (this.Operator, (this.Precedence, this.IsLeftAssociative)) = (op, OperatorNode.Defs[op]);

        public OperatorClass Class => (this.Operator == Operator.UnaryMinus || this.Operator == Operator.UnaryPlus || this.Operator == Operator.Not) ? OperatorClass.Unary : OperatorClass.Binary;

        public static Operator? ConvertOperator(TokenType token, bool unary) {
            if (!unary) {
                switch (token) {
                    case TokenType.Plus: return Operator.Addition;
                    case TokenType.Minus: return Operator.Subtraction;
                    case TokenType.Asterisk: return Operator.Multiplication;
                    case TokenType.ForwardSlash: return Operator.Division;
                    case TokenType.Percent: return Operator.Remainder;
                    case TokenType.Caret: return Operator.Exponentiation;
                    case TokenType.DoubleLessThan: return Operator.ShiftLeft;
                    case TokenType.DoubleGreaterThan: return Operator.ShiftRight;
                    case TokenType.TripleLessThan: return Operator.RotateLeft;
                    case TokenType.TripleGreaterThan: return Operator.RotateRight;
                    case TokenType.Ampersand: return Operator.And;
                    case TokenType.Pipe: return Operator.Or;
                    case TokenType.Tilde: return Operator.Xor;
                    case TokenType.ExclamationPointAmpersand: return Operator.NotAnd;
                    case TokenType.ExclamationPointPipe: return Operator.NotOr;
                    case TokenType.ExclamationPointTilde: return Operator.NotXor;
                    case TokenType.DoubleEqual: return Operator.Equals;
                    case TokenType.ExclamationPointEqual: return Operator.NotEquals;
                    case TokenType.LessThan: return Operator.LessThan;
                    case TokenType.LessThanEqual: return Operator.LessThanOrEqual;
                    case TokenType.GreaterThan: return Operator.GreaterThan;
                    case TokenType.GreaterThanEqual: return Operator.GreaterThanOrEqual;
                    case TokenType.OpenParenthesis: return Operator.OpenParenthesis;
                    case TokenType.CloseParenthesis: return Operator.CloseParenthesis;
                    default: return null;
                }
            }
            else {
                switch (token) {
                    case TokenType.Plus: return Operator.UnaryPlus;
                    case TokenType.Minus: return Operator.UnaryMinus;
                    case TokenType.ExclamationPoint: return Operator.Not;
                    default: return null;
                }
            }
        }

        public static Operator? ConvertCompoundOperator(TokenType token) {
            switch (token) {
                case TokenType.PlusEqual: return Operator.Addition;
                case TokenType.MinusEqual: return Operator.Subtraction;
                case TokenType.AsteriskEqual: return Operator.Multiplication;
                case TokenType.ForwardSlashEqual: return Operator.Division;
                case TokenType.PercentEqual: return Operator.Remainder;
                case TokenType.CaretEqual: return Operator.Exponentiation;
                case TokenType.DoubleLessThanEqual: return Operator.ShiftLeft;
                case TokenType.DoubleGreaterThanEqual: return Operator.ShiftRight;
                case TokenType.TripleLessThanEqual: return Operator.RotateLeft;
                case TokenType.TripleGreaterThanEqual: return Operator.RotateRight;
                case TokenType.AmpersandEqual: return Operator.And;
                case TokenType.PipeEqual: return Operator.Or;
                case TokenType.TildeEqual: return Operator.Xor;
                case TokenType.ExclamationPointAmpersandEqual: return Operator.NotAnd;
                case TokenType.ExclamationPointPipeEqual: return Operator.NotOr;
                case TokenType.ExclamationPointTildeEqual: return Operator.NotXor;
                default: return null;
            }
        }
    }
}
