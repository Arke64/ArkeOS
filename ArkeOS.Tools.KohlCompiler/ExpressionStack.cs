using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class ExpressionStack {
        private static IReadOnlyDictionary<Operator, int> Precedences { get; } = new Dictionary<Operator, int> { [Operator.Addition] = 0, [Operator.Subtraction] = 0, [Operator.Multiplication] = 1, [Operator.Division] = 1, [Operator.Remainder] = 1, [Operator.Exponentiation] = 2, [Operator.UnaryMinus] = 3, [Operator.UnaryPlus] = 3, [Operator.OpenParenthesis] = -1, [Operator.CloseParenthesis] = -1 };
        private static IReadOnlyDictionary<Operator, bool> LeftAssociative { get; } = new Dictionary<Operator, bool> { [Operator.Addition] = true, [Operator.Subtraction] = true, [Operator.Multiplication] = true, [Operator.Division] = true, [Operator.Remainder] = true, [Operator.Exponentiation] = false, [Operator.UnaryMinus] = false, [Operator.UnaryPlus] = false, [Operator.OpenParenthesis] = true, [Operator.CloseParenthesis] = true };

        private readonly Stack<ExpressionNode> outputStack = new Stack<ExpressionNode>();
        private readonly Stack<(OperatorNode, PositionInfo)> operatorStack = new Stack<(OperatorNode, PositionInfo)>();

        private Operator PeekOperator() => this.operatorStack.Peek().Item1.Operator;

        public void Push(NumberNode node) => this.outputStack.Push(node);
        public void Push(IdentifierNode node) => this.outputStack.Push(node);

        public void Push(OperatorNode node, PositionInfo position) {
            var op = node.Operator;

            if (op == Operator.CloseParenthesis) {
                while (this.operatorStack.Any() && this.PeekOperator() != Operator.OpenParenthesis)
                    this.Reduce();

                this.operatorStack.Pop();
            }
            else if (op == Operator.OpenParenthesis) {
                this.operatorStack.Push((node, position));
            }
            else {
                while (this.operatorStack.Any() && ((ExpressionStack.Precedences[this.PeekOperator()] > ExpressionStack.Precedences[op]) || (ExpressionStack.Precedences[this.PeekOperator()] == ExpressionStack.Precedences[op] && ExpressionStack.LeftAssociative[op])))
                    this.Reduce();

                this.operatorStack.Push((node, position));
            }
        }

        public ExpressionNode ToNode() {
            while (this.operatorStack.Any())
                this.Reduce();

            return this.outputStack.SingleOrDefault();
        }

        private void Reduce() {
            var op = this.operatorStack.Pop();

            switch (OperatorNode.GetOperatorClass(op.Item1.Operator)) {
                case OperatorClass.Binary:
                    if (this.outputStack.Count < 2) throw new ExpectedTokenException(op.Item2, "operand");

                    var r = this.outputStack.Pop();
                    var l = this.outputStack.Pop();

                    this.outputStack.Push(new BinaryExpressionNode(l, op.Item1, r));

                    break;

                case OperatorClass.Unary:
                    if (this.outputStack.Count < 1) throw new ExpectedTokenException(op.Item2, "operand");

                    this.outputStack.Push(new UnaryExpressionNode(op.Item1, this.outputStack.Pop()));

                    break;
            }
        }
    }
}
