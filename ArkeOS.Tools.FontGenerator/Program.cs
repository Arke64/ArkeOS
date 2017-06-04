using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ArkeOS.Tools.FontGenerator {
    public class Program {
        private const int CharacterWidth = 5;
        private const int CharacterHeight = 8;

        public static void Main(string[] args) {
            if (args.Length == 0 || !File.Exists(args[0])) {
                Console.WriteLine("Usage: [input file name]");

                return;
            }

            //TODO Remove CoreCompat reference once .NET Standard 2.0 adds System.Drawing
            using (var bmp = new Bitmap(args[0])) {
                var final = "";

                for (var i = 0; i < 95; i++) {
                    var bin = new List<Color>(Program.CharacterHeight * Program.CharacterWidth);

                    for (var y = 0; y < Program.CharacterHeight; y++)
                        for (var x = 0; x < Program.CharacterWidth; x++)
                            bin.Add(bmp.GetPixel(x + i * Program.CharacterWidth, y).R == 0 ? Color.White : Color.FromArgb(0, 0, 0, 0));

                    final += "			this.fontData['" + (char)(' ' + i) + "'] = new uint[] { " + string.Join(",", bin.Select(b => "0x" + Convert.ToString(b.ToArgb(), 16).ToUpper())) + " };\r\n";
                }

                Console.Write(final);
            }
        }
    }
}
