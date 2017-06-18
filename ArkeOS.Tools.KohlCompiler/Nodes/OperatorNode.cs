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
        OpenParenthesis,
        CloseParenthesis,
    }

    public enum OperatorClass {
        Unary,
        Binary,
    }

    public class OperatorNode : Node {
        public Operator Operator { get; }

        public OperatorNode(Operator op) => this.Operator = op;

        public static OperatorClass GetOperatorClass(Operator op) => op == Operator.UnaryMinus || op == Operator.UnaryPlus ? OperatorClass.Unary : OperatorClass.Binary;
    }
}
