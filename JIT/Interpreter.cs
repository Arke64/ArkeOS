using System.Linq;
using ArkeOS.Executable;

namespace ArkeOS.Interpreter {
	public class Interpreter {
		private Image image;

		public Interpreter() {
			this.image = new Image();
		}

		public void Parse(byte[] data) {
			this.image.FromArray(data);

			if (this.image.Header.Magic != Header.MagicNumber) throw new InvalidProgramFormatException();
			if (!this.image.Sections.Any()) throw new InvalidProgramFormatException();
		}

		public void Run() {

		}
	}
}