using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler {
    public class Compiler {
        private readonly List<string> files = new List<string>();

        public string OutputName { get; set; } = "Kohl.bin";
        public bool Optimize { get; set; } = true;
        public bool EmitAssemblyListing { get; set; } = false;
        public bool EmitBootable { get; set; } = true;

        public void AddSource(string file) => this.files.Add(file);

        public void Compile() {
            var lexer = new Lexer(this.files);
            var parser = new Parser(lexer);
            var emitter = new Emitter(parser.Parse());

            emitter.Emit(this.OutputName);
        }
    }
}
