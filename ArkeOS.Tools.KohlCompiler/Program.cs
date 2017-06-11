﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.Assembler {
    public static class Program {
        public static void Main(string[] args) {
            var source = "1 * (2 + 3) - 9 * 7 / (4 - -3) + +3 ^ (12 + 3 + (5 % 1) / (6 - -(2 - 3)) * 3)"; //387,420,485

            source = "4 + 10 - 6 * 4 / 2 % 3 ^ 2"; //11
            //source = "3 ^ 2 ^ 3"; // 6561

            var lexer = new Lexer(source);
            var tokens = lexer.Lex();
            var parser = new Parser(tokens);
            var tree = parser.Parse();
            var emitter = new Emitter(tree);
            emitter.Emit();
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
            if (this.index < this.length) {
                value = this.source[this.index++];

                return true;
            }
            else {
                value = '\0';

                return false;
            }
        }

        private void Add(TokenType type, char c) => this.result.Add(new Token { Type = type, Value = c.ToString() });
        private void Add(TokenType type, string s) => this.result.Add(new Token { Type = type, Value = s });

        public Lexer(string source) => (this.source, this.length) = (source, source.Length);

        public IReadOnlyList<Token> Lex() {
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
                    case ' ': break;
                    default:
                        if (char.IsNumber(c)) this.Add(TokenType.Number, this.ReadString(c, char.IsDigit));
                        else if (char.IsLetter(c)) this.Add(TokenType.String, this.ReadString(c, char.IsLetter));
                        else throw new InvalidOperationException($"Unexpected '{c}'");

                        break;
                }
            }

            return this.result;
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

        public NumberNode(string value) : base(null, null) => this.Value = int.Parse(value);
    }

    public class OperationNode : Node {
        public Operation Operation { get; }

        public OperationNode(Node left, Node right, Operation operation) : base(left, right) => this.Operation = operation;
    }

    public class Parser {
        private static IReadOnlyDictionary<Operation, uint> Precedences { get; } = new Dictionary<Operation, uint> { [Operation.Addition] = 0, [Operation.Subtraction] = 0, [Operation.Multiplication] = 1, [Operation.Division] = 1, [Operation.Remainder] = 1, [Operation.Exponentiation] = 2 };
        private static IReadOnlyDictionary<Operation, bool> LeftAssociative { get; } = new Dictionary<Operation, bool> { [Operation.Addition] = true, [Operation.Subtraction] = true, [Operation.Multiplication] = true, [Operation.Division] = true, [Operation.Remainder] = true, [Operation.Exponentiation] = false };

        private readonly IReadOnlyList<Token> tokens;
        private readonly int length;
        private int index = 0;

        private bool Peek(out Token value) {
            if (this.index < this.length) {
                value = this.tokens[this.index];

                return true;
            }
            else {
                value = default(Token);

                return false;
            }
        }

        private bool Read(out Token value) {
            if (this.index < this.length) {
                value = this.tokens[this.index++];

                return true;
            }
            else {
                value = default(Token);

                return false;
            }
        }

        public Parser(IReadOnlyList<Token> tokens) => (this.tokens, this.length) = (tokens, tokens.Count);

        public Node Parse() {
            var res = this.ReadExpression();

            if (this.index != this.length)
                throw new InvalidOperationException("Unexpected end.");

            return res;
        }

        private Node ReadExpression() {
            if (!this.Peek(out var token) || token.Type != TokenType.Number) throw new InvalidOperationException("Expected token.");

            var outputStack = new Stack<Node>();
            var operatorStack = new Stack<Operation>();
            var cont = true;

            do {
                switch (token.Type) {
                    default: cont = false; break;
                    case TokenType.Number: outputStack.Push(new NumberNode(token.Value)); break;
                    case TokenType.Plus:
                    case TokenType.Minus:
                    case TokenType.Asterisk:
                    case TokenType.ForwardSlash:
                    case TokenType.Percent:
                    case TokenType.Caret:
                        var op = (Operation)token.Type;

                        while (operatorStack.Any() && ((Parser.Precedences[operatorStack.Peek()] > Parser.Precedences[op]) || (Parser.Precedences[operatorStack.Peek()] == Parser.Precedences[op] && Parser.LeftAssociative[op])))
                            reduce();

                        operatorStack.Push(op);

                        break;
                }

                if (cont)
                    this.Read(out _);
            } while (cont && this.Peek(out token));

            while (operatorStack.Any())
                reduce();

            return outputStack.Single();

            void reduce()
            {
                var r = outputStack.Pop();
                var l = outputStack.Pop();

                outputStack.Push(new OperationNode(l, r, operatorStack.Pop()));
            }
        }
    }

    public class Emitter {
        private readonly Node tree;

        public Emitter(Node tree) => this.tree = tree;

        public void Emit() => Console.WriteLine(this.Calculate(this.tree).ToString("N0"));

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