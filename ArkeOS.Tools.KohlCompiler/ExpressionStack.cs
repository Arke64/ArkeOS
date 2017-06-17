using ArkeOS.Tools.KohlCompiler.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
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

    public class ExpressionStack {
        private static IReadOnlyDictionary<Operator, int> Precedences { get; } = new Dictionary<Operator, int> { [Operator.Addition] = 0, [Operator.Subtraction] = 0, [Operator.Multiplication] = 1, [Operator.Division] = 1, [Operator.Remainder] = 1, [Operator.Exponentiation] = 2, [Operator.UnaryMinus] = 3, [Operator.UnaryPlus] = 3, [Operator.OpenParenthesis] = -1, [Operator.CloseParenthesis] = -1 };
        private static IReadOnlyDictionary<Operator, bool> LeftAssociative { get; } = new Dictionary<Operator, bool> { [Operator.Addition] = true, [Operator.Subtraction] = true, [Operator.Multiplication] = true, [Operator.Division] = true, [Operator.Remainder] = true, [Operator.Exponentiation] = false, [Operator.UnaryMinus] = false, [Operator.UnaryPlus] = false, [Operator.OpenParenthesis] = true, [Operator.CloseParenthesis] = true };

        private readonly Stack<Node> outputStack = new Stack<Node>();
        private readonly Stack<Operator> operatorStack = new Stack<Operator>();

        public void Push(NumberNode node) => this.outputStack.Push(node);
        public void Push(IdentifierNode node) => this.outputStack.Push(node);

        public void Push(Operator op) {
            if (op == Operator.CloseParenthesis) {
                while (this.operatorStack.Any() && this.operatorStack.Peek() != Operator.OpenParenthesis)
                    this.Reduce();

                this.operatorStack.Pop();
            }
            else if (op == Operator.OpenParenthesis) {
                this.operatorStack.Push(op);
            }
            else {
                while (this.operatorStack.Any() && ((ExpressionStack.Precedences[this.operatorStack.Peek()] > ExpressionStack.Precedences[op]) || (ExpressionStack.Precedences[this.operatorStack.Peek()] == ExpressionStack.Precedences[op] && ExpressionStack.LeftAssociative[op])))
                    this.Reduce();

                this.operatorStack.Push(op);
            }
        }

        public Node ToNode() {
            while (this.operatorStack.Any())
                this.Reduce();

            return this.outputStack.Single();
        }

        private void Reduce() {
            var op = this.operatorStack.Pop();

            if (!UnaryOperationNode.IsValidOperator(op)) {
                var r = this.outputStack.Pop();
                var l = this.outputStack.Pop();

                this.outputStack.Push(new BinaryOperationNode(l, r, op));
            }
            else {
                this.outputStack.Push(new UnaryOperationNode(this.outputStack.Pop(), op));
            }
        }
    }
}