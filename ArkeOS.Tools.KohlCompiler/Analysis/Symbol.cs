using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public abstract class Symbol : IEquatable<Symbol> {
        public string Name { get; }

        protected Symbol(string name) => this.Name = name;

        public override string ToString() => $"{this.Name}({this.GetType().Name})";

        public static bool operator ==(Symbol lhs, Symbol rhs) => lhs?.Name == rhs?.Name;
        public static bool operator !=(Symbol lhs, Symbol rhs) => !(lhs == rhs);
        public override int GetHashCode() => this.Name.GetHashCode();
        public override bool Equals(object obj) => obj is Symbol t && this.Equals(t);
        public bool Equals(Symbol obj) => this == obj;
    }

    public sealed class FunctionSymbol : Symbol {
        private List<ArgumentSymbol> arguments;
        private List<LocalVariableSymbol> localVariables;

        public TypeSymbol Type { get; }
        public IReadOnlyList<ArgumentSymbol> Arguments => this.arguments;
        public IReadOnlyList<LocalVariableSymbol> LocalVariables => this.localVariables;

        public ulong StackRequired => (ulong)(this.Arguments.Count + this.LocalVariables.Count);

        public FunctionSymbol(string name, TypeSymbol type, IReadOnlyList<ArgumentSymbol> arguments, IReadOnlyList<LocalVariableSymbol> variables) : base(name) => (this.Type, this.arguments, this.localVariables) = (type, arguments.ToList(), variables.ToList());

        public void AddLocalVariable(LocalVariableSymbol variable) => this.localVariables.Add(variable);

        public ulong GetPosition(ArgumentSymbol sym) => FunctionSymbol.GetPosition(this.arguments, sym);
        public ulong GetPosition(LocalVariableSymbol sym) => FunctionSymbol.GetPosition(this.localVariables, sym);

        private static ulong GetPosition<T>(List<T> source, T sym) where T : Symbol {
            var idx = source.IndexOf(sym);

            return idx != -1 ? (ulong)idx : throw new IdentifierNotFoundException(default(PositionInfo), sym.Name);
        }
    }

    public sealed class ArgumentSymbol : Symbol {
        public TypeSymbol Type { get; }

        public ArgumentSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class LocalVariableSymbol : Symbol {
        public TypeSymbol Type { get; }

        public LocalVariableSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class RegisterSymbol : Symbol {
        public Register Register { get; }

        public RegisterSymbol(string name, Register register) : base(name) => this.Register = register;
    }

    public sealed class GlobalVariableSymbol : Symbol {
        public TypeSymbol Type { get; }

        public GlobalVariableSymbol(string name, TypeSymbol type) : base(name) => this.Type = type;
    }

    public sealed class ConstVariableSymbol : Symbol {
        public TypeSymbol Type { get; }
        public ulong Value { get; }

        public ConstVariableSymbol(string name, TypeSymbol type, ulong value) : base(name) => (this.Type, this.Value) = (type, value);
    }

    public sealed class TypeSymbol : Symbol {
        public string BaseName { get; }
        public IReadOnlyCollection<TypeSymbol> TypeArguments { get; }

        public TypeSymbol(string name) : this(name, new List<TypeSymbol>()) { }
        public TypeSymbol(string name, IReadOnlyCollection<TypeSymbol> typeArguments) : base(name + (typeArguments.Any() ? $"[{string.Join(", ", typeArguments.Select(g => g.Name))}]" : string.Empty)) => (this.BaseName, this.TypeArguments) = (name, typeArguments);
    }
}
