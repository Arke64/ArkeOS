using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            var source = "1 * (2 + 3) - 9 * 7 / (4 - -3) + +3 ^ (12 + 3 + (5 % 1) / (6 - -(2 - 3)) * 3)"; //387,420,485

            Console.WriteLine(new Dictionary<string, int> {
                ["3 ^ 2 ^ 3"] = 6561,
                ["4 + 10 - 6 * 4 / 2 % 3 ^ 2"] = 11,
                ["-4 - -10 + 2 - 4 + +2"] = 6,
            }.All(t => new Compiler(t.Key).Compile() == t.Value));
        }
    }

    public class Compiler {
        private readonly string source;

        public Compiler(string source) => this.source = source;

        public int Compile() {
            var lexer = new Lexer(this.source);
            var tokens = lexer.Lex();
            var parser = new Parser(tokens);
            var tree = parser.Parse();
            var emitter = new Emitter(tree);

            return emitter.Emit();
        }
    }

    public enum TokenType {
        Number,
        String,
        Asterisk,
        Plus,
        Minus,
        ForwardSlash,
        Percent,
        Caret,
        OpenParenthesis,
        CloseParenthesis,
    }

    public struct Token {
        public TokenType Type;
        public string Value;

        public override string ToString() => $"{this.Type}{(this.Type == TokenType.Number || this.Type == TokenType.String ? ": " + this.Value : string.Empty)}";
    }

    public class Lexer {
        private readonly List<Token> result = new List<Token>();
        private readonly string source;
        private readonly int length;
        private int index = 0;

        private bool Peek(out char value) {
            if (this.index < this.length) {
                value = this.source[this.index];

                return true;
            }
            else {
                value = '\0';

                return false;
            }
        }

        private bool Read(out char value) {
            if (this.Peek(out value)) {
                this.index++;

                return true;
            }

            return false;
        }

        private void Add(TokenType type, char c) => this.result.Add(new Token { Type = type, Value = c.ToString() });
        private void Add(TokenType type, string s) => this.result.Add(new Token { Type = type, Value = s });

        public Lexer(string source) => (this.source, this.length) = (source, source.Length);

        public TokenStream Lex() {
            while (this.Read(out var c)) {
                switch (c) {
                    case '+': this.Add(TokenType.Plus, c); break;
                    case '-': this.Add(TokenType.Minus, c); break;
                    case '*': this.Add(TokenType.Asterisk, c); break;
                    case '/': this.Add(TokenType.ForwardSlash, c); break;
                    case '^': this.Add(TokenType.Caret, c); break;
                    case '%': this.Add(TokenType.Percent, c); break;
                    case '(': this.Add(TokenType.OpenParenthesis, c); break;
                    case ')': this.Add(TokenType.CloseParenthesis, c); break;
                    default:
                        if (char.IsNumber(c)) this.Add(TokenType.Number, this.ReadString(c, char.IsDigit));
                        else if (char.IsLetter(c)) this.Add(TokenType.String, this.ReadString(c, char.IsLetter));
                        else if (char.IsWhiteSpace(c)) continue;
                        else throw new InvalidOperationException($"Unexpected '{c}'");

                        break;
                }
            }

            return new TokenStream(this.result);
        }

        private string ReadString(char start, Func<char, bool> validator) {
            var res = new StringBuilder();

            res.Append(start);

            while (this.Peek(out var c) && validator(c)) {
                this.Read(out _);

                res.Append(c);
            }

            return res.ToString();
        }
    }

    public class TokenStream {
        private readonly IReadOnlyList<Token> tokens;
        private readonly int length;
        private int index = 0;

        public TokenStream(IReadOnlyList<Token> tokens) => (this.tokens, this.length) = (tokens, tokens.Count);

        public int Remaining => this.length - this.index;

        public bool Peek(out Token value) {
            if (this.index < this.length) {
                value = this.tokens[this.index];
                return true;
            }
            else {
                value = default(Token);
                return false;
            }
        }

        public bool Read(out Token value) {
            if (this.Peek(out value)) {
                this.index++;

                return true;
            }

            return false;
        }

        public bool Peek(Func<Token, bool> validator, out Token value) => this.Peek(out value) && validator(value);

        public bool Read(Func<Token, bool> validator, out Token value) => this.Read(out value) && validator(value);

        public bool Read(TokenType type, out Token value) => this.Read(t => t.Type == type, out value);

        public T Read<T>(TokenType t1, Func<Token, T> case1, TokenType t2, Func<Token, T> case2, TokenType t3, Func<Token, T> case3) {
            if (this.Peek(out var token)) {
                if (token.Type == t1) { this.Read(out _); return case1(token); }
                else if (token.Type == t2) { this.Read(out _); return case2(token); }
                else if (token.Type == t3) { this.Read(out _); return case3(token); }
            }

            throw new InvalidOperationException("Expected token.");
        }
    }

    public enum Operation {
        Addition = TokenType.Plus,
        Subtraction = TokenType.Minus,
        Multiplication = TokenType.Asterisk,
        Division = TokenType.ForwardSlash,
        Remainder = TokenType.Percent,
        Exponentiation = TokenType.Caret,
    }

    public abstract class Node {
        public Node Left { get; }
        public Node Right { get; }

        protected Node(Node left, Node right) => (this.Left, this.Right) = (left, right);
    }

    public class NumberNode : Node {
        public int Value { get; }

        public NumberNode(Token token) : this(token, false) { }
        public NumberNode(Token token, bool isNegative) : base(null, null) => this.Value = int.Parse(token.Value) * (isNegative ? -1 : 1);
    }

    public class OperationNode : Node {
        public Operation Operation { get; }

        public OperationNode(Node left, Node right, Operation operation) : base(left, right) => this.Operation = operation;
    }

    public class Parser {
        private static IReadOnlyDictionary<Operation, uint> Precedences { get; } = new Dictionary<Operation, uint> { [Operation.Addition] = 0, [Operation.Subtraction] = 0, [Operation.Multiplication] = 1, [Operation.Division] = 1, [Operation.Remainder] = 1, [Operation.Exponentiation] = 2 };
        private static IReadOnlyDictionary<Operation, bool> LeftAssociative { get; } = new Dictionary<Operation, bool> { [Operation.Addition] = true, [Operation.Subtraction] = true, [Operation.Multiplication] = true, [Operation.Division] = true, [Operation.Remainder] = true, [Operation.Exponentiation] = false };

        private readonly TokenStream tokens;

        public Parser(TokenStream tokens) => this.tokens = tokens;

        public Node Parse() {
            var res = this.ReadExpression();

            if (this.tokens.Remaining > 0)
                throw new InvalidOperationException("Unexpected end.");

            return res;
        }

        private Node ReadExpression() {
            var stack = new ExpressionStack();

            stack.Push(this.ReadNumber());

            while (this.tokens.Peek(this.IsOperation, out _)) {
                stack.Push(this.ReadOperation());

                stack.Push(this.ReadNumber());
            }

            return stack.ToNode();
        }

        private Operation ReadOperation() => this.tokens.Read(this.IsOperation, out var token) ? (Operation)token.Type : throw new InvalidOperationException("Expected token");

        private NumberNode ReadNumber() => this.tokens.Read(
                TokenType.Plus, t => new NumberNode(this.ReadOnlyNumber()),
                TokenType.Minus, t => new NumberNode(this.ReadOnlyNumber(), true),
                TokenType.Number, t => new NumberNode(t));

        private Token ReadOnlyNumber() => this.tokens.Read(TokenType.Number, out var t) ? t : throw new InvalidOperationException("Expected token");

        private bool IsOperation(Token token) {
            switch (token.Type) {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Percent:
                case TokenType.Caret:
                    return true;
            }

            return false;
        }
    }

    public class ExpressionStack {
        private static IReadOnlyDictionary<Operation, uint> Precedences { get; } = new Dictionary<Operation, uint> { [Operation.Addition] = 0, [Operation.Subtraction] = 0, [Operation.Multiplication] = 1, [Operation.Division] = 1, [Operation.Remainder] = 1, [Operation.Exponentiation] = 2 };
        private static IReadOnlyDictionary<Operation, bool> LeftAssociative { get; } = new Dictionary<Operation, bool> { [Operation.Addition] = true, [Operation.Subtraction] = true, [Operation.Multiplication] = true, [Operation.Division] = true, [Operation.Remainder] = true, [Operation.Exponentiation] = false };

        private readonly Stack<Node> outputStack = new Stack<Node>();
        private readonly Stack<Operation> operatorStack = new Stack<Operation>();

        public void Push(NumberNode node) => this.outputStack.Push(node);

        public void Push(Operation op) {
            while (this.operatorStack.Any() && ((ExpressionStack.Precedences[this.operatorStack.Peek()] > ExpressionStack.Precedences[op]) || (ExpressionStack.Precedences[this.operatorStack.Peek()] == ExpressionStack.Precedences[op] && ExpressionStack.LeftAssociative[op])))
                this.Reduce();

            this.operatorStack.Push(op);
        }

        public Node ToNode() {
            while (this.operatorStack.Any())
                this.Reduce();

            return this.outputStack.Single();
        }

        private void Reduce() {
            var r = this.outputStack.Pop();
            var l = this.outputStack.Pop();

            this.outputStack.Push(new OperationNode(l, r, this.operatorStack.Pop()));
        }
    }

    public class Emitter {
        private readonly Node tree;

        public Emitter(Node tree) => this.tree = tree;

        public int Emit() => this.Calculate(this.tree);

        private int Calculate(Node node) {
            switch (node) {
                case NumberNode n:
                    return n.Value;

                case OperationNode n:
                    var a = this.Calculate(n.Left);
                    var b = this.Calculate(n.Right);

                    switch (n.Operation) {
                        case Operation.Addition: return a + b;
                        case Operation.Subtraction: return a - b;
                        case Operation.Multiplication: return a * b;
                        case Operation.Division: return a / b;
                        case Operation.Exponentiation: return (int)Math.Pow(a, b);
                        case Operation.Remainder: return a % b;
                        default: throw new InvalidOperationException("Unexpected operation.");
                    }

                default:
                    throw new InvalidOperationException("Unexpected node.");
            }
        }
    }
}