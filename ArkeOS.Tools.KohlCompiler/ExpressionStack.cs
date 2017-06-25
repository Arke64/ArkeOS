using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class ExpressionStack {
        private readonly Stack<ExpressionNode> outputStack = new Stack<ExpressionNode>();
        private readonly Stack<(OperatorNode, PositionInfo)> operatorStack = new Stack<(OperatorNode, PositionInfo)>();

        private OperatorNode PeekOperator() => this.operatorStack.Peek().Item1;

        public void Push(ExpressionNode node) => this.outputStack.Push(node);

        public void Push(OperatorNode node, PositionInfo position) {
            if (node.Operator == Operator.CloseParenthesis) {
                while (this.operatorStack.Any() && this.PeekOperator().Operator != Operator.OpenParenthesis)
                    this.Reduce();

                this.operatorStack.Pop();
            }
            else if (node.Operator == Operator.OpenParenthesis) {
                this.operatorStack.Push((node, position));
            }
            else {
                while (this.operatorStack.Any() && ((this.PeekOperator().Precedence > node.Precedence) || (this.PeekOperator().Precedence == node.Precedence && node.IsLeftAssociative)))
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

            switch (op.Item1.Class) {
                case OperatorClass.Binary:
                    if (this.outputStack.Count < 2) throw new ExpectedException(op.Item2, "operand");

                    var r = this.outputStack.Pop();
                    var l = this.outputStack.Pop();

                    this.outputStack.Push(new BinaryExpressionNode(l, op.Item1, r));

                    break;

                case OperatorClass.Unary:
                    if (this.outputStack.Count < 1) throw new ExpectedException(op.Item2, "operand");

                    this.outputStack.Push(new UnaryExpressionNode(op.Item1, this.outputStack.Pop()));

                    break;
            }
        }
    }
}
