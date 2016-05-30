using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FontGenerator {
	class Program {
		static void Main(string[] args) {
			var bmp = new Bitmap("Untitled.bmp");
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
