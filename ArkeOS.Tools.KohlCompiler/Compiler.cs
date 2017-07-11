using ArkeOS.Tools.KohlCompiler.Emit;
using ArkeOS.Tools.KohlCompiler.Exceptions;
using ArkeOS.Tools.KohlCompiler.IR;
using ArkeOS.Tools.KohlCompiler.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public sealed class CompilationError {
        public PositionInfo Position { get; }
        public string Message { get; }

        public CompilationError(PositionInfo position, string message) => (this.Position, this.Message) = (position, message);

        public override string ToString() => $"'{this.Position.File}' at {this.Position.Line:N0}:{this.Position.Column:N0}: {this.Message}";
    }

    public sealed class CompilationOptions {
        private readonly List<string> sources = new List<string>();

        public IReadOnlyCollection<string> Sources => this.sources;

        public string OutputName { get; set; } = "Kohl.bin";
        public bool Optimize { get; set; } = false;
        public bool EmitAssemblyListing { get; set; } = false;
        public bool EmitBootable { get; set; } = false;

        public void AddSource(string file) => this.sources.Add(file);

        public static (CompilationOptions result, IReadOnlyCollection<string> unrecognized) FromArgs(string[] args) {
            var a = new Queue<string>(args);
            var c = new CompilationOptions();
            var e = new List<string>();

            while (a.Any()) {
                var cur = a.Dequeue().Substring(2);

                switch (cur) {
                    case "bootable": c.EmitBootable = true; break;
                    case "optimize": c.Optimize = true; break;
                    case "asm": c.EmitAssemblyListing = true; break;
                    case "output": c.OutputName = a.Dequeue(); break;
                    case "src": c.AddSource(a.Dequeue()); break;
                    default: e.Add(cur); break;
                }
            }

            return (c, e);
        }
    }

    public static class Compiler {
        public static IReadOnlyCollection<CompilationError> Compile(CompilationOptions options) {
            var errors = new List<CompilationError>();

            try {
                var ast = Parser.Parse(options);
                var ir = IrGenerator.Generate(ast);

                Emitter.Emit(options, ir);
            }
            catch (CompilationException e) {
                errors.Add(new CompilationError(e.Position, e.Message));

                if (Debugger.IsAttached)
                    throw;
            }

            return errors;
        }
    }
}
