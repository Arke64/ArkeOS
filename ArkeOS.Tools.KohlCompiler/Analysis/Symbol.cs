using ArkeOS.Hardware.Architecture;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler.Analysis {
    public abstract class Symbol {
        public string Name { get; }

        protected Symbol(string name) => this.Name = name;

        public override string ToString() => $"{this.Name}({this.GetType().Name})";
    }

    public sealed class FunctionSymbol : Symbol {
        private List<ArgumentSymbol> arguments;
        private List<LocalVariableSymbol> localVariables;

        public TypeSymbol Type { get; }
        public IReadOnlyCollection<ArgumentSymbol> Arguments => this.arguments;
        public IReadOnlyCollection<LocalVariableSymbol> LocalVariables => this.localVariables;

        public FunctionSymbol(string name, TypeSymbol type, IReadOnlyCollection<ArgumentSymbol> arguments, IReadOnlyCollection<LocalVariableSymbol> variables) : base(name) => (this.Type, this.arguments, this.localVariables) = (type, arguments.ToList(), variables.ToList());

        public void AddLocalVariable(LocalVariableSymbol variable) => this.localVariables.Add(variable);
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
        public IReadOnlyCollection<TypeSymbol> GenericArguments { get; }

        public string FullName => this.Name + (this.GenericArguments.Any() ? "[" + string.Join(", ", this.GenericArguments.Select(g => g.Name)) + "]" : string.Empty);

        public TypeSymbol(string name) : base(name) => this.GenericArguments = new List<TypeSymbol>();
        public TypeSymbol(string name, params TypeSymbol[] genericArguments) : base(name) => this.GenericArguments = genericArguments;

        public static bool operator ==(TypeSymbol lhs, TypeSymbol rhs) => lhs.FullName == rhs.FullName;
        public static bool operator !=(TypeSymbol lhs, TypeSymbol rhs) => !(lhs == rhs);
        public override bool Equals(object obj) => obj is TypeSymbol t && t == this;
        public override int GetHashCode() => this.FullName.GetHashCode();
    }
}
