using System.IO;

namespace ArkeOS.Tools.KohlCompiler {
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
}
