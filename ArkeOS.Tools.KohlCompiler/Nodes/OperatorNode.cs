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

    public class OperatorNode : Node {
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

        public static OperatorClass GetOperatorClass(Operator op) => (op == Operator.UnaryMinus || op == Operator.UnaryPlus || op == Operator.Not) ? OperatorClass.Unary : OperatorClass.Binary;
    }
}
