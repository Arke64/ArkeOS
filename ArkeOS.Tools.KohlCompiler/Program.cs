﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main() => Console.WriteLine(new List<(string source, int result)> {
            ("3 ^ 2 ^ 3", 6561),
            ("4 + 10 - 6 * 4 / 2 % 3 ^ 2", 11),
            ("-4 - -10 + 2 - 4 + +2", 6),
            ("0xAB_cD + 1 + -0b010_1 + 0d_12345_", 56322),
            ("1 * (2 + 3) - 9 * 7 / (4 - -3) + +3 ^ (12 + 3 + (11 % 6) / (6 - -(2 - 3)) * 3)", 387_420_485),
        }.All(t => new Compiler(t.source).Compile() == t.result));
    }

    public class Compiler {
        private readonly string source;

        public Compiler(string source) => this.source = source;

        public long Compile() {
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
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

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
                        if (char.IsNumber(c)) this.Add(TokenType.Number, this.ReadNumber(c));
                        else if (char.IsLetter(c)) this.Add(TokenType.String, this.ReadIdentifier(c));
                        else if (char.IsWhiteSpace(c)) continue;
                        else throw new InvalidOperationException($"Unexpected '{c}'");

                        break;
                }
            }

            return new TokenStream(this.result);
        }

        private string ReadNumber(char start) {
            var res = new StringBuilder();
            var radix = 10;

            if (start == '0' && this.Peek(out var c)) {
                switch (c) {
                    case 'd': radix = 10; this.Read(out _); break;
                    case 'x': radix = 16; this.Read(out _); break;
                    case 'b': radix = 2; this.Read(out _); break;
                    case 'o': radix = 8; this.Read(out _); break;
                    default: res.Append(start); break;
                }
            }
            else {
                res.Append(start);
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.Peek(out c) && (valid.Contains(c) || c == '_')) {
                this.Read(out _);

                if (c != '_')
                    res.Append(c);
            }

            return Convert.ToUInt64(res.ToString(), radix).ToString();
        }

        private string ReadIdentifier(char start) {
            var res = new StringBuilder();

            res.Append(start);

            while (this.Peek(out var c) && char.IsLetterOrDigit(c)) {
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

        public bool Peek(TokenType type, out Token value) => this.Peek(t => t.Type == type, out value);
        public bool Read(TokenType type, out Token value) => this.Read(t => t.Type == type, out value);

        public bool Peek(TokenType type) => this.Peek(type, out _);
        public bool Read(TokenType type) => this.Read(type, out _);
    }

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

    public abstract class Node {

    }

    public class NumberNode : Node {
        public string Value { get; }

        public NumberNode(string value) => this.Value = value;
    }

    public class BinaryOperationNode : Node {
        public Node Left { get; }
        public Node Right { get; }
        public Operator Operator { get; }

        public BinaryOperationNode(Node left, Node right, Operator op) => (this.Left, this.Right, this.Operator) = (left, right, op);
    }

    public class UnaryOperationNode : Node {
        public Node Node { get; }
        public Operator Operator { get; }

        public UnaryOperationNode(Node node, Operator op) => (this.Node, this.Operator) = (node, op);

        public static bool IsValidOperator(Operator op) => op == Operator.UnaryMinus || op == Operator.UnaryPlus;
    }

    public class Parser {
        private static IReadOnlyDictionary<Operator, uint> Precedences { get; } = new Dictionary<Operator, uint> { [Operator.Addition] = 0, [Operator.Subtraction] = 0, [Operator.Multiplication] = 1, [Operator.Division] = 1, [Operator.Remainder] = 1, [Operator.Exponentiation] = 2 };
        private static IReadOnlyDictionary<Operator, bool> LeftAssociative { get; } = new Dictionary<Operator, bool> { [Operator.Addition] = true, [Operator.Subtraction] = true, [Operator.Multiplication] = true, [Operator.Division] = true, [Operator.Remainder] = true, [Operator.Exponentiation] = false };

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
            var state = ExpressionParseState.Start;
            var cont = true;

            while (cont && this.tokens.Peek(out var token)) {
                switch (state) {
                    case ExpressionParseState.Start:
                        if (token.Type == TokenType.Number) {
                            stack.Push(this.ReadNumber());
                            state = ExpressionParseState.FoundNumber;
                        }
                        else if (token.Type == TokenType.OpenParenthesis) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundOpenParenthesis;
                        }
                        else if (token.Type == TokenType.CloseParenthesis) {
                            throw new InvalidOperationException("Expected token.");
                        }
                        else if (this.IsOperator(token)) {
                            stack.Push(this.ReadOperator(true));
                            state = ExpressionParseState.FoundOperator;
                        }
                        else {
                            cont = false;
                        }

                        break;

                    case ExpressionParseState.FoundNumber:
                        if (token.Type == TokenType.CloseParenthesis) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundCloseParenthesis;
                        }
                        else if (this.IsOperator(token)) {
                            if (token.Type == TokenType.Percent)
                                token = token;
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundOperator;
                        }
                        else {
                            cont = false;
                        }

                        break;

                    case ExpressionParseState.FoundOperator:
                        if (token.Type == TokenType.Number) {
                            stack.Push(this.ReadNumber());
                            state = ExpressionParseState.FoundNumber;
                        }
                        else if (token.Type == TokenType.OpenParenthesis) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundOpenParenthesis;
                        }
                        else if (token.Type == TokenType.CloseParenthesis) {
                            throw new InvalidOperationException("Expected token.");
                        }
                        else if (this.IsOperator(token)) {
                            stack.Push(this.ReadOperator(true));
                            state = ExpressionParseState.FoundOperator;
                        }
                        else {
                            cont = false;
                        }

                        break;

                    case ExpressionParseState.FoundOpenParenthesis:
                        if (token.Type == TokenType.Number) {
                            stack.Push(this.ReadNumber());
                            state = ExpressionParseState.FoundNumber;
                        }
                        else if (token.Type == TokenType.OpenParenthesis) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundOpenParenthesis;
                        }
                        else if (token.Type == TokenType.CloseParenthesis) {
                            throw new InvalidOperationException("Expected token.");
                        }
                        else if (this.IsOperator(token)) {
                            stack.Push(this.ReadOperator(true));
                            state = ExpressionParseState.FoundOperator;
                        }
                        else {
                            cont = false;
                        }

                        break;

                    case ExpressionParseState.FoundCloseParenthesis:
                        if (token.Type == TokenType.Number) {
                            throw new InvalidOperationException("Expected token.");
                        }
                        else if (token.Type == TokenType.OpenParenthesis) {
                            throw new InvalidOperationException("Expected token.");
                        }
                        else if (token.Type == TokenType.CloseParenthesis) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundCloseParenthesis;
                        }
                        else if (this.IsOperator(token)) {
                            stack.Push(this.ReadOperator(false));
                            state = ExpressionParseState.FoundOperator;
                        }
                        else {
                            cont = false;
                        }

                        break;
                }
            }

            return stack.ToNode();
        }

        private Operator ReadOperator(bool unary) => this.tokens.Read(this.IsOperator, out var token) ? this.GetOperator(token, unary) : throw new InvalidOperationException("Expected token");

        private NumberNode ReadNumber() => this.tokens.Read(TokenType.Number, out var token) ? new NumberNode(token.Value) : throw new InvalidOperationException("Expected token");

        private bool IsOperator(Token token) {
            switch (token.Type) {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Percent:
                case TokenType.Caret:
                case TokenType.OpenParenthesis:
                case TokenType.CloseParenthesis:
                    return true;
            }

            return false;
        }

        private Operator GetOperator(Token token, bool unary) {
            if (!unary) {
                switch (token.Type) {
                    case TokenType.Plus: return Operator.Addition;
                    case TokenType.Minus: return Operator.Subtraction;
                    case TokenType.Asterisk: return Operator.Multiplication;
                    case TokenType.ForwardSlash: return Operator.Division;
                    case TokenType.Percent: return Operator.Remainder;
                    case TokenType.Caret: return Operator.Exponentiation;
                    case TokenType.OpenParenthesis: return Operator.OpenParenthesis;
                    case TokenType.CloseParenthesis: return Operator.CloseParenthesis;
                }
            }
            else {
                switch (token.Type) {
                    case TokenType.Plus: return Operator.UnaryPlus;
                    case TokenType.Minus: return Operator.UnaryMinus;
                }
            }

            throw new InvalidOperationException("Expected token");
        }

        private enum ExpressionParseState {
            Start,
            FoundNumber,
            FoundOperator,
            FoundOpenParenthesis,
            FoundCloseParenthesis,
        }
    }

    public class ExpressionStack {
        private static IReadOnlyDictionary<Operator, int> Precedences { get; } = new Dictionary<Operator, int> { [Operator.Addition] = 0, [Operator.Subtraction] = 0, [Operator.Multiplication] = 1, [Operator.Division] = 1, [Operator.Remainder] = 1, [Operator.Exponentiation] = 2, [Operator.UnaryMinus] = 3, [Operator.UnaryPlus] = 3, [Operator.OpenParenthesis] = -1, [Operator.CloseParenthesis] = -1 };
        private static IReadOnlyDictionary<Operator, bool> LeftAssociative { get; } = new Dictionary<Operator, bool> { [Operator.Addition] = true, [Operator.Subtraction] = true, [Operator.Multiplication] = true, [Operator.Division] = true, [Operator.Remainder] = true, [Operator.Exponentiation] = false, [Operator.UnaryMinus] = false, [Operator.UnaryPlus] = false, [Operator.OpenParenthesis] = true, [Operator.CloseParenthesis] = true };

        private readonly Stack<Node> outputStack = new Stack<Node>();
        private readonly Stack<Operator> operatorStack = new Stack<Operator>();

        public void Push(NumberNode node) => this.outputStack.Push(node);

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

    public class Emitter {
        private readonly Node tree;

        public Emitter(Node tree) => this.tree = tree;

        public long Emit() => this.Calculate(this.tree);

        private long Calculate(Node node) {
            switch (node) {
                case NumberNode n:
                    return long.Parse(n.Value);

                case BinaryOperationNode n:
                    var l = this.Calculate(n.Left);
                    var r = this.Calculate(n.Right);

                    switch (n.Operator) {
                        case Operator.Addition: return l + r;
                        case Operator.Subtraction: return l - r;
                        case Operator.Multiplication: return l * r;
                        case Operator.Division: return l / r;
                        case Operator.Exponentiation: return (int)Math.Pow(l, r);
                        case Operator.Remainder: return l % r;
                        default: throw new InvalidOperationException("Unexpected operator.");
                    }

                case UnaryOperationNode n:
                    var o = this.Calculate(n.Node);

                    switch (n.Operator) {
                        case Operator.UnaryPlus: return o;
                        case Operator.UnaryMinus: return -o;
                        default: throw new InvalidOperationException("Unexpected operator.");
                    }


                default:
                    throw new InvalidOperationException("Unexpected node.");
            }
        }
    }
}