using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public class Compiler {
        private readonly IList<string> files = new List<string>();

        public string OutputName { get; set; } = "Kohl.bin";
        public bool Optimize { get; set; } = true;
        public bool EmitAssemblyListing { get; set; } = false;
        public bool EmitBootable { get; set; } = true;

        public void AddSource(string file) => this.files.Add(file);

        public void Compile() {
            var sources = this.files.Select(f => File.ReadAllText(f));

            var lexer = new Lexer(sources.First());
            var tokens = lexer.GetStream();
            var parser = new Parser(tokens);
            var tree = parser.Parse();
            var emitter = new Emitter(tree);

            emitter.Emit(this.OutputName);
        }
    }
}
