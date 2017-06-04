using System;
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
                    var bin = "";

                    for (var y = 0; y < 8; y++)
                        for (var x = 0; x < 5; x++)
                            bin += bmp.GetPixel(x + i * 5, y).R == 0 ? "1" : "0";

                    final += "			this.fontData['" + (char)(' ' + i) + "'] = 0x" + Convert.ToUInt64(new string(bin.Reverse().ToArray()), 2).ToString("X") + ";\r\n";
                }

                Console.Write(final);
            }
        }
    }
}
