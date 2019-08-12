using ArkeOS.Tools.KohlCompiler.Analysis;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public abstract class RValue {

    }

    public sealed class IntegerRValue : RValue {
        public ulong Value { get; }

        public IntegerRValue(ulong value) => this.Value = value;

        public override string ToString() => this.Value.ToString();
    }

    public sealed class AddressOfRValue : RValue {
        public LValue Target { get; }

        public AddressOfRValue(LValue target) => this.Target = target;

        public override string ToString() => $"addr {this.Target}";
    }

    public abstract class LValue : RValue {

    }

    public sealed class ArgumentLValue : LValue {
        public ArgumentSymbol Symbol { get; }

        public ArgumentLValue(ArgumentSymbol argument) => this.Symbol = argument;

        public override string ToString() => $"aref {this.Symbol}";
    }

    public sealed class LocalVariableLValue : LValue {
        public LocalVariableSymbol Symbol { get; }

        public LocalVariableLValue(LocalVariableSymbol variable) => this.Symbol = variable;

        public override string ToString() => $"vref {this.Symbol}";
    }

    public sealed class GlobalVariableLValue : LValue {
        public GlobalVariableSymbol Symbol { get; }

        public GlobalVariableLValue(GlobalVariableSymbol variable) => this.Symbol = variable;

        public override string ToString() => $"gref {this.Symbol}";
    }

    public sealed class FunctionLValue : LValue {
        public FunctionSymbol Symbol { get; }

        public FunctionLValue(FunctionSymbol function) => this.Symbol = function;

        public override string ToString() => $"fref {this.Symbol}";
    }

    public sealed class PointerLValue : LValue {
        public RValue Target { get; }

        public PointerLValue(RValue target) => this.Target = target;

        public override string ToString() => $"pref {this.Target}";
    }

    public sealed class RegisterLValue : LValue {
        public RegisterSymbol Symbol { get; }

        public RegisterLValue(RegisterSymbol symbol) => this.Symbol = symbol;

        public override string ToString() => $"rref {this.Symbol}";
    }

    public sealed class StructMemberLValue : LValue {
        public RValue Target { get; }
        public StructMemberSymbol Member { get; }

        public StructMemberLValue(RValue target, StructMemberSymbol member) => (this.Target, this.Member) = (target, member);

        public override string ToString() => $"strc {this.Target}.{this.Member}";
    }
}
