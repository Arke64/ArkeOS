using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class ExpressionStack {
        private readonly Stack<ExpressionStatementNode> outputStack = new Stack<ExpressionStatementNode>();
        private readonly Stack<OperatorNode> operatorStack = new Stack<OperatorNode>();

        public void Push(ExpressionStatementNode node) => this.outputStack.Push(node);

        public void Push(OperatorNode node) {
            if (node.Operator == Operator.CloseParenthesis) {
                while (this.operatorStack.Any() && this.operatorStack.Peek().Operator != Operator.OpenParenthesis)
                    this.Reduce();

                this.operatorStack.Pop();
            }
            else if (node.Operator == Operator.OpenParenthesis) {
                this.operatorStack.Push(node);
            }
            else {
                while (this.operatorStack.Any() && ((this.operatorStack.Peek().Precedence > node.Precedence) || (this.operatorStack.Peek().Precedence == node.Precedence && node.IsLeftAssociative)))
                    this.Reduce();

                this.operatorStack.Push(node);
            }
        }

        public ExpressionStatementNode ToNode() {
            while (this.operatorStack.Any())
                this.Reduce();

            return this.outputStack.SingleOrDefault();
        }

        private void Reduce() {
            var op = this.operatorStack.Pop();

            switch (op.Class) {
                case OperatorClass.Binary:
                    if (this.outputStack.Count < 2) throw new ExpectedException(op.Position, "operand");

                    var r = this.outputStack.Pop();
                    var l = this.outputStack.Pop();

                    this.outputStack.Push(new BinaryExpressionNode(l.Position, l, op, r));

                    break;

                case OperatorClass.Unary:
                    if (this.outputStack.Count < 1) throw new ExpectedException(op.Position, "operand");

                    this.outputStack.Push(new UnaryExpressionNode(op.Position, op, this.outputStack.Pop()));

                    break;
            }
        }
    }
}
