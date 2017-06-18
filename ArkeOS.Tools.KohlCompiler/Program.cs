using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkeOS.Tools.KohlCompiler {
    public static class Program {
        public static void Main(string[] args) {
            var a = new Queue<string>(args);

            if (a.Count < 2) {
                Console.WriteLine("Usage: [output file] [source files...]");

                return;
            }

            var compiler = new Compiler { OutputName = a.Dequeue() };

            while (a.Any())
                compiler.AddSource(a.Dequeue());

            compiler.Compile();
        }
    }
}
