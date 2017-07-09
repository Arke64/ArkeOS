using ArkeOS.Tools.KohlCompiler.Analysis;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public abstract class Terminator {

    }

    public sealed class ReturnTerminator : Terminator {
        public RValue Value { get; }

        public ReturnTerminator(RValue value) => this.Value = value;

        public override string ToString() => $"return {this.Value}";
    }

    public sealed class GotoTerminator : Terminator {
        public BasicBlock Next { get; private set; }

        public void SetNext(BasicBlock next) => this.Next = next;

        public override string ToString() => $"goto";
    }

    public sealed class IfTerminator : Terminator {
        public RValue Condition { get; }
        public BasicBlock NextTrue { get; private set; }
        public BasicBlock NextFalse { get; private set; }

        public IfTerminator(RValue condition) => this.Condition = condition;

        public void SetNext(BasicBlock nextTrue, BasicBlock nextFalse) => (this.NextTrue, this.NextFalse) = (nextTrue, nextFalse);

        public override string ToString() => $"if {this.Condition}";
    }

    public sealed class CallTerminator : Terminator {
        public FunctionSymbol Target { get; }
        public LValue Result { get; }
        public IReadOnlyCollection<RValue> Arguments { get; }
        public BasicBlock Next { get; private set; }

        public CallTerminator(FunctionSymbol target, LValue result, IReadOnlyCollection<RValue> arguments) => (this.Target, this.Result, this.Arguments) = (target, result, arguments);

        public void SetNext(BasicBlock next) => this.Next = next;

        public override string ToString() => $"{this.Result} = call {this.Target}({string.Join(", ", this.Arguments)})";
    }
}
