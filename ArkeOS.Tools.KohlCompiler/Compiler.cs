using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class CompilationError {
        public string Message { get; }
        public PositionInfo Position { get; }

        public CompilationError(string message, PositionInfo position) => (this.Message, this.Position) = (message, position);

        public override string ToString() => $"'{this.Position.File}' at {this.Position.Line:N0}:{this.Position.Column:N0}: {this.Message}";
    }

    public class Compiler {
        private readonly List<string> sources = new List<string>();

        public string OutputName { get; set; } = "Kohl.bin";
        public bool Optimize { get; set; } = true;
        public bool EmitAssemblyListing { get; set; } = true;
        public bool EmitBootable { get; set; } = false;

        public void AddSource(string file) => this.sources.Add(file);

        public IReadOnlyList<CompilationError> Compile() {
            var errors = new List<CompilationError>();

            try {
                var lexer = new Lexer(this.sources);
                var parser = new Parser(lexer);
                var ast = parser.Parse();
                var ir = IrGenerator.LowerIr(ast);
                var emitter = new Emitter(ir, this.EmitAssemblyListing, this.EmitBootable, this.OutputName);

                emitter.Emit();
            }
            catch (CompilationException e) {
                errors.Add(new CompilationError(e.Message, e.Position));

                if (Debugger.IsAttached)
                    throw;
            }

            return errors;
        }

        public static string Usage { get; } = "[optional options] [output file] [source files...]";

        public static Compiler FromArgs(string[] args) {
            var a = new Queue<string>(args);
            var c = new Compiler();

            if (a.Count < 2) return null;

            while (a.Peek().StartsWith("--")) {
                switch (a.Dequeue().Substring(2)) {
                    case "bootable": c.EmitBootable = true; break;
                    default: return null;
                }
            }

            c.OutputName = a.Dequeue();

            while (a.Any())
                c.AddSource(a.Dequeue());

            return c;
        }
    }
}
