using ArkeOS.Hardware.Architecture;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public abstract class Symbol : IEquatable<Symbol> {
        public string Name { get; }
        public TypeSymbol Type { get; }

        protected Symbol(string name, TypeSymbol type) => (this.Name, this.Type) = (name, type);

        public override string ToString() => $"{this.Name}({this.GetType().Name})";

        public static bool operator ==(Symbol lhs, Symbol rhs) => lhs?.Name == rhs?.Name;
        public static bool operator !=(Symbol lhs, Symbol rhs) => !(lhs == rhs);
        public override int GetHashCode() => this.Name.GetHashCode();
        public override bool Equals(object obj) => obj is Symbol t && this.Equals(t);
        public bool Equals(Symbol obj) => this == obj;
    }

    public sealed class StructSymbol : TypeSymbol {
        private readonly List<StructMemberSymbol> members = new List<StructMemberSymbol>();

        public IReadOnlyList<StructMemberSymbol> Members => this.members;

        public override ulong Size => (ulong)this.Members.Sum(m => (long)m.Type.Size);

        public StructSymbol(string name) : base(name, 0) { }

        public void AddMember(StructMemberSymbol member) => this.members.Add(member);
    }

    public sealed class FunctionSymbol : Symbol {
        private List<ArgumentSymbol> arguments;
        private List<LocalVariableSymbol> localVariables;
        private ulong argumentsStackSize;

        public IReadOnlyList<ArgumentSymbol> Arguments => this.arguments;
        public IReadOnlyList<LocalVariableSymbol> LocalVariables => this.localVariables;

        public ulong StackRequired => (ulong)(this.Arguments.Sum(v => (long)v.Type.Size) + this.LocalVariables.Sum(v => (long)v.Type.Size));

        public FunctionSymbol(string name, TypeSymbol type, IReadOnlyList<ArgumentSymbol> arguments, IReadOnlyList<LocalVariableSymbol> variables) : base(name, type) => (this.arguments, this.localVariables, this.argumentsStackSize) = (arguments.ToList(), variables.ToList(), (ulong)arguments.Sum(a => (long)a.Type.Size));

        public void AddLocalVariable(LocalVariableSymbol variable) => this.localVariables.Add(variable);

        public ulong GetStackPosition(ArgumentSymbol sym) => FunctionSymbol.GetPosition(this.arguments, sym);
        public ulong GetStackPosition(LocalVariableSymbol sym) => FunctionSymbol.GetPosition(this.localVariables, sym) + this.argumentsStackSize;

        private static ulong GetPosition<T>(List<T> source, T sym) where T : Symbol {
            var idx = source.IndexOf(sym);

            if (idx == -1) throw new IdentifierNotFoundException(default(PositionInfo), sym.Name);

            return (ulong)source.Take(idx).Sum(i => (long)i.Type.Size);
        }
    }

    public sealed class ArgumentSymbol : Symbol {
        public ArgumentSymbol(string name, TypeSymbol type) : base(name, type) { }
    }

    public sealed class LocalVariableSymbol : Symbol {
        public LocalVariableSymbol(string name, TypeSymbol type) : base(name, type) { }
    }

    public sealed class StructMemberSymbol : Symbol {
        public ulong Offset { get; }

        public StructMemberSymbol(string name, TypeSymbol type, ulong offset) : base(name, type) => this.Offset = offset;
    }

    public sealed class RegisterSymbol : Symbol {
        public Register Register { get; }

        public RegisterSymbol(string name, Register register) : base(name, WellKnownSymbol.Word) => this.Register = register;
    }

    public sealed class GlobalVariableSymbol : Symbol {
        public GlobalVariableSymbol(string name, TypeSymbol type) : base(name, type) { }
    }

    public sealed class ConstVariableSymbol : Symbol {
        public ulong Value { get; }

        public ConstVariableSymbol(string name, TypeSymbol type, ulong value) : base(name, type) => this.Value = value;
    }

    public class TypeSymbol : Symbol {
        public string BaseName { get; }
        public virtual ulong Size { get; }
        public IReadOnlyCollection<TypeSymbol> TypeArguments { get; }

        public TypeSymbol(string name, ulong size) : this(name, size, new List<TypeSymbol>()) { }
        public TypeSymbol(string name, ulong size, IReadOnlyCollection<TypeSymbol> typeArguments) : base(name + (typeArguments.Any() ? $"[{string.Join(", ", typeArguments.Select(g => g.Name))}]" : string.Empty), null) => (this.BaseName, this.Size, this.TypeArguments) = (name, size, typeArguments);
    }
}
