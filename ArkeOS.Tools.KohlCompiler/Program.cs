using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            args = new[] { @"..\Images\Kohl.k" };

            if (args.Length < 1) {
                Console.WriteLine("Need at least one argument: the file to assemble");

                return;
            }

            var input = args[0];

            if (!File.Exists(input)) {
                Console.WriteLine("The specified file cannot be found.");

                return;
            }

            Compiler.Compile(input);
        }
    }

    public static class Compiler {
        public static void Compile(string file) {
            var source = File.ReadAllText(file);

            var lexer = new Lexer(source);
            var tokens = lexer.GetStream();
            var parser = new Parser(tokens);
            var tree = parser.Parse();
            var emitter = new Emitter(tree);

            emitter.Emit(Path.ChangeExtension(file, "bin"));
        }
    }

    public enum TokenType {
        Number,
        Identifier,
        Asterisk,
        Plus,
        Minus,
        ForwardSlash,
        Percent,
        Caret,
        OpenParenthesis,
        CloseParenthesis,
        EqualsSign,
        Semicolon,
    }

    public struct Token {
        public TokenType Type;
        public string Value;

        public Token(TokenType type, string value) => (this.Type, this.Value) = (type, value);
        public Token(TokenType type, char value) => (this.Type, this.Value) = (type, value.ToString());

        public override string ToString() => $"{this.Type}{(this.Type == TokenType.Number || this.Type == TokenType.Identifier ? ": " + this.Value : string.Empty)}";
    }

    public class Lexer {
        private static IReadOnlyDictionary<int, char[]> ValidDigitsForBase { get; } = new Dictionary<int, char[]> { [2] = new[] { '0', '1' }, [8] = new[] { '0', '1', '2', '3', '4', '5', '6', '7' }, [10] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, [16] = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };

        private readonly TokenStream stream;
        private readonly string source;
        private readonly int length;
        private int index;
        private Token current;
        private bool currentValid;

        public Lexer(string source) {
            this.stream = new TokenStream(this);
            this.source = source;
            this.length = source.Length;
            this.index = 0;

            this.LexNextToken();
        }

        private void Advance() => this.index++;

        private bool Peek(out char value) {
            if (this.index < this.length) {
                value = this.source[this.index];

                return true;
            }
            else {
                value = default(char);

                return false;
            }
        }

        private string ReadIdentifier() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && (char.IsLetterOrDigit(c) || c == '_')) {
                this.Advance();

                res.Append(c);
            }

            return res.ToString();
        }

        private string ReadNumber() {
            var res = new StringBuilder();
            var radix = 10;

            if (this.Peek(out var s) && s == '0') {
                this.Advance();

                if (this.Peek(out var c)) {
                    switch (c) {
                        case 'd': radix = 10; this.Advance(); break;
                        case 'x': radix = 16; this.Advance(); break;
                        case 'b': radix = 2; this.Advance(); break;
                        case 'o': radix = 8; this.Advance(); break;
                        default: res.Append(s); break;
                    }
                }
                else {
                    return "0";
                }
            }

            var valid = Lexer.ValidDigitsForBase[radix];

            while (this.Peek(out var c) && (valid.Contains(c) || c == '_')) {
                this.Advance();

                if (c != '_')
                    res.Append(c);
            }

            return Convert.ToUInt64(res.ToString(), radix).ToString();
        }

        private string ReadWhitespace() {
            var res = new StringBuilder();

            while (this.Peek(out var c) && char.IsWhiteSpace(c)) {
                this.Advance();

                res.Append(c);
            }

            return res.ToString();
        }

        private void LexNextToken() {
            if (this.Peek(out var c)) {
                if (char.IsLetter(c)) {
                    this.current = new Token(TokenType.Identifier, this.ReadIdentifier());
                }
                else if (char.IsNumber(c)) {
                    this.current = new Token(TokenType.Number, this.ReadNumber());
                }
                else if (char.IsWhiteSpace(c)) {
                    this.ReadWhitespace();

                    this.LexNextToken();

                    return;
                }
                else {
                    switch (c) {
                        case '+': this.current = new Token(TokenType.Plus, c); break;
                        case '-': this.current = new Token(TokenType.Minus, c); break;
                        case '*': this.current = new Token(TokenType.Asterisk, c); break;
                        case '/': this.current = new Token(TokenType.ForwardSlash, c); break;
                        case '^': this.current = new Token(TokenType.Caret, c); break;
                        case '%': this.current = new Token(TokenType.Percent, c); break;
                        case '(': this.current = new Token(TokenType.OpenParenthesis, c); break;
                        case ')': this.current = new Token(TokenType.CloseParenthesis, c); break;
                        case '=': this.current = new Token(TokenType.EqualsSign, c); break;
                        case ';': this.current = new Token(TokenType.Semicolon, c); break;
                        default: throw new InvalidOperationException($"Unexpected '{c}'");
                    }

                    this.Advance();
                }

                this.currentValid = true;
            }
            else {
                this.current = default(Token);
                this.currentValid = false;
            }
        }

        public bool ReadNext(out Token token) {
            var res = this.PeekNext(out token);

            if (res)
                this.LexNextToken();

            return res;
        }

        public bool PeekNext(out Token token) {
            token = this.current;

            return this.currentValid;
        }

        public TokenStream GetStream() => this.stream;
    }

    public class TokenStream {
        private readonly Lexer lexer;

        public TokenStream(Lexer lexer) => this.lexer = lexer;

        public bool Peek(out Token value) => this.lexer.PeekNext(out value);
        public bool Read(out Token value) => this.lexer.ReadNext(out value);

        public bool Peek(Func<Token, bool> validator, out Token value) => this.Peek(out value) && validator(value);
        public bool Read(Func<Token, bool> validator, out Token value) => this.Read(out value) && validator(value);

        public bool Peek(TokenType type, out Token value) => this.Peek(t => t.Type == type, out value);
        public bool Read(TokenType type, out Token value) => this.Read(t => t.Type == type, out value);

        public bool Peek(TokenType type) => this.Peek(type, out _);
        public bool Read(TokenType type) => this.Read(type, out _);

        public List<Token> ToList() {
            var res = new List<Token>();

            while (this.Read(out var t))
                res.Add(t);

            return res;
        }
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

    public class IdentifierNode : Node {
        public string Identifier { get; }

        public IdentifierNode(string identifier) => this.Identifier = identifier;
    }

    public class NumberNode : Node {
        public long Number { get; }

        public NumberNode(string number) => this.Number = long.Parse(number);
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

    public class AssignmentNode : Node {
        public IdentifierNode Target { get; }
        public Node Value { get; }

        public AssignmentNode(IdentifierNode identifier, Node value) => (this.Target, this.Value) = (identifier, value);
    }

    public class ProgramNode : Node {
        private List<AssignmentNode> assignments = new List<AssignmentNode>();

        public IReadOnlyList<AssignmentNode> Assignments => this.assignments;

        public void Add(AssignmentNode node) => this.assignments.Add(node);
    }

    public class Parser {
        private static IReadOnlyDictionary<Operator, uint> Precedences { get; } = new Dictionary<Operator, uint> { [Operator.Addition] = 0, [Operator.Subtraction] = 0, [Operator.Multiplication] = 1, [Operator.Division] = 1, [Operator.Remainder] = 1, [Operator.Exponentiation] = 2 };
        private static IReadOnlyDictionary<Operator, bool> LeftAssociative { get; } = new Dictionary<Operator, bool> { [Operator.Addition] = true, [Operator.Subtraction] = true, [Operator.Multiplication] = true, [Operator.Division] = true, [Operator.Remainder] = true, [Operator.Exponentiation] = false };

        private readonly TokenStream tokens;

        public Parser(TokenStream tokens) => this.tokens = tokens;

        public ProgramNode Parse() {
            var res = new ProgramNode();

            while (this.tokens.Peek(out _))
                res.Add(this.ReadAssignment());

            return res;
        }

        private AssignmentNode ReadAssignment() {
            var ident = this.ReadIdentifier();
            this.tokens.Read(TokenType.EqualsSign);
            var exp = this.ReadExpression();
            this.tokens.Read(TokenType.Semicolon);

            return new AssignmentNode(ident, exp);
        }

        private Node ReadExpression() {
            var stack = new ExpressionStack();
            var start = true;

            while (this.tokens.Peek(out var token)) {
                if (start && (token.Type == TokenType.Number || token.Type == TokenType.Identifier)) {
                    if (token.Type == TokenType.Number)
                        stack.Push(this.ReadNumber());
                    else
                        stack.Push(this.ReadIdentifier());

                    start = false;
                }
                else if (!start && token.Type == TokenType.CloseParenthesis) {
                    stack.Push(this.ReadOperator(false));
                    start = false;
                }
                else if (this.IsOperator(token)) {
                    stack.Push(this.ReadOperator(start && token.Type != TokenType.OpenParenthesis));
                    start = true;
                }
                else {
                    break;
                }
            }

            return stack.ToNode();
        }

        private Operator ReadOperator(bool unary) => this.tokens.Read(this.IsOperator, out var token) ? this.GetOperator(token, unary) : throw new InvalidOperationException("Expected token");

        private NumberNode ReadNumber() => this.tokens.Read(TokenType.Number, out var token) ? new NumberNode(token.Value) : throw new InvalidOperationException("Expected token");
        private IdentifierNode ReadIdentifier() => this.tokens.Read(TokenType.Identifier, out var token) ? new IdentifierNode(token.Value) : throw new InvalidOperationException("Expected token");

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

    public class Emitter {
        private static Parameter StackParam { get; } = new Parameter { Type = ParameterType.Stack };

        private readonly List<Instruction> instructions = new List<Instruction>();
        private readonly ProgramNode tree;

        public Emitter(ProgramNode tree) => this.tree = tree;

        public void Emit(string outputFile) {
            var start = new Parameter { Type = ParameterType.Literal, Literal = 0, IsRIPRelative = true };
            var len = new Parameter { Type = ParameterType.Literal, Literal = 0 };

            this.instructions.Add(new Instruction(InstructionDefinition.Find("CPY").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = Register.RZERO }, start, len }, null, false));
            this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = Register.RIP }, new Parameter { Type = ParameterType.Register, Register = Register.RZERO } }, null, false));

            start.Literal = (ulong)this.instructions.Sum(i => i.Length);

            this.Visit(this.tree);

            this.instructions.Add(new Instruction(InstructionDefinition.Find("HLT").Code, new List<Parameter> { }, null, false));

            len.Literal = (ulong)this.instructions.Sum(i => i.Length) - start.Literal;

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(0x00000000454B5241UL);

                    foreach (var inst in this.instructions)
                        inst.Encode(writer);

                    File.WriteAllBytes(Path.ChangeExtension(outputFile, "bin"), stream.ToArray());
                }
            }
        }

        private void Visit(Node node) {
            switch (node) {
                case ProgramNode n:
                    foreach (var a in n.Assignments)
                        this.Visit(a);

                    break;

                case AssignmentNode n:
                    this.Visit(n.Value);

                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { new Parameter { Type = ParameterType.Register, Register = n.Target.Identifier.ToEnum<Register>() }, Emitter.StackParam }, null, false));

                    break;

                case NumberNode n:
                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { Emitter.StackParam, new Parameter { Type = ParameterType.Literal, Literal = (ulong)n.Number } }, null, false));

                    break;

                case IdentifierNode n:
                    this.instructions.Add(new Instruction(InstructionDefinition.Find("SET").Code, new List<Parameter> { Emitter.StackParam, new Parameter { Type = ParameterType.Register, Register = n.Identifier.ToEnum<Register>() } }, null, false));

                    break;

                case BinaryOperationNode n:
                    this.Visit(n.Left);
                    this.Visit(n.Right);

                    var inst = "";

                    switch (n.Operator) {
                        case Operator.Addition: inst = "ADD"; break;
                        case Operator.Subtraction: inst = "SUB"; break;
                        case Operator.Multiplication: inst = "MUL"; break;
                        case Operator.Division: inst = "DIV"; break;
                        case Operator.Exponentiation: inst = "POW"; break;
                        case Operator.Remainder: inst = "MOD"; break;
                    }

                    this.instructions.Add(new Instruction(InstructionDefinition.Find(inst).Code, new List<Parameter> { Emitter.StackParam, Emitter.StackParam, Emitter.StackParam }, null, false));

                    break;

                case UnaryOperationNode n:
                    this.Visit(n.Node);

                    switch (n.Operator) {
                        case Operator.UnaryPlus: break;
                        case Operator.UnaryMinus: this.instructions.Add(new Instruction(InstructionDefinition.Find("MUL").Code, new List<Parameter> { Emitter.StackParam, Emitter.StackParam, new Parameter { Type = ParameterType.Literal, Literal = unchecked((ulong)(-1)) } }, null, false)); break;
                        default: throw new InvalidOperationException("Unexpected operator.");
                    }

                    break;

                default:
                    throw new InvalidOperationException("Unexpected node.");
            }

        }
    }
}