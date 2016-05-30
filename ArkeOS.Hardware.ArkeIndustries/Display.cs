using System.Text;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.ArkeIndustries {
	public class Display : SystemBusDevice {
		private const int CharacterWidth = 5;
		private const int CharacterHeight = 8;

		private ulong height;
		private ulong width;
		private ulong rows;
		private ulong columns;
		private char[] characterBuffer;
		private char[] decodeBuffer;
		private byte[] encodeBuffer;
		private ulong[] fontData;

		public byte[] RawBuffer { get; private set; }

		public Display(int width, int height) : base(ProductIds.Vendor, ProductIds.D100, DeviceType.Display) {
			this.RawBuffer = new byte[width * height * 4];

			this.width = (ulong)width;
			this.height = (ulong)height;
			this.columns = this.width / Display.CharacterWidth;
			this.rows = this.height / Display.CharacterHeight;
			this.characterBuffer = new char[this.columns * this.rows];
			this.decodeBuffer = new char[8];
			this.encodeBuffer = new byte[8];

			this.fontData = new ulong[256];
			this.SetFontData();
		}

		public override unsafe ulong ReadWord(ulong address) {
			if (address == 0) {
				return this.columns;
			}
			else if (address == 1) {
				return this.rows;
			}
			else if (address == 2) {
				return this.width;
			}
			else if (address == 3) {
				return this.height;
			}
			else if (address == 4) {
				return Display.CharacterWidth;
			}
			else if (address == 5) {
				return Display.CharacterHeight;
			}
			else if (address >= 0x100000) {
				address -= 0x100000;

				Encoding.UTF8.GetBytes(this.characterBuffer, (int)address, 1, this.encodeBuffer, 0);

				fixed (byte* b = this.encodeBuffer)
					return *(ulong*)b;
			}
			else {
				return 0;
			}
		}

		public override unsafe void WriteWord(ulong address, ulong data) {
			if (address < 0x100000)
				return;

			address -= 0x100000;

			fixed (byte* b = this.encodeBuffer)
				*(ulong*)b = data;

			Encoding.UTF8.GetChars(this.encodeBuffer, 0, 8, this.decodeBuffer, 0);

			this.characterBuffer[address] = this.decodeBuffer[0];

			var pixelData = this.fontData[this.characterBuffer[address]];

			fixed (byte* b = this.RawBuffer) {
				var column = address % this.columns;
				var row = address / this.columns;
				var pixels = (uint*)b + row * this.width * Display.CharacterHeight + column * Display.CharacterWidth;

				for (var r = 0; r < Display.CharacterHeight; r++)
					for (var c = 0; c < Display.CharacterWidth; c++)
						pixels[r * (int)this.width + c] = (pixelData & (1UL << (r * Display.CharacterWidth + c))) != 0 ? 0x00FFFFFFU : 0x0U;
			}
		}

		private void SetFontData() {
			this.fontData[' '] = 0x0;
			this.fontData['!'] = 0x80210842;
			this.fontData['"'] = 0x10054A0;
			this.fontData['#'] = 0x2AFAFEA0;
			this.fontData['$'] = 0x8E4380E2;
			this.fontData['%'] = 0x12222400;
			this.fontData['&'] = 0x594454A4C;
			this.fontData['\''] = 0x2100;
			this.fontData['('] = 0x410842200;
			this.fontData[')'] = 0x221084100;
			this.fontData['*'] = 0x288E2000;
			this.fontData['+'] = 0x8E2000;
			this.fontData[','] = 0x22000;
			this.fontData['-'] = 0xE0000;
			this.fontData['.'] = 0x318000000;
			this.fontData['/'] = 0x4318618C2;
			this.fontData['0'] = 0x325AF5A4C;
			this.fontData['1'] = 0x1084210C4;
			this.fontData['2'] = 0x3C2222107;
			this.fontData['3'] = 0x1D083A107;
			this.fontData['4'] = 0x21087A54C;
			this.fontData['5'] = 0x1D083042F;
			this.fontData['6'] = 0x192938426;
			this.fontData['7'] = 0x84422107;
			this.fontData['8'] = 0x19297A526;
			this.fontData['9'] = 0x190872526;
			this.fontData[':'] = 0x6300C60;
			this.fontData[';'] = 0x466018C0;
			this.fontData['<'] = 0x208208888;
			this.fontData['='] = 0xF03C00;
			this.fontData['>'] = 0x44441041;
			this.fontData['?'] = 0x40109C87;
			this.fontData['@'] = 0x3C2D6B52F;
			this.fontData['A'] = 0x25297A526;
			this.fontData['B'] = 0x1D297A527;
			this.fontData['C'] = 0x38210842E;
			this.fontData['D'] = 0x3F318C72F;
			this.fontData['E'] = 0x3C217842F;
			this.fontData['F'] = 0x4217842F;
			this.fontData['G'] = 0x39296842E;
			this.fontData['H'] = 0x25297A529;
			this.fontData['I'] = 0x38842108E;
			this.fontData['J'] = 0x3D2942108;
			this.fontData['K'] = 0x24A308CA9;
			this.fontData['L'] = 0x3C2108421;
			this.fontData['M'] = 0x25297BDE9;
			this.fontData['N'] = 0x35AD5AD6B;
			this.fontData['O'] = 0x3D294A52F;
			this.fontData['P'] = 0x4217A52F;
			this.fontData['Q'] = 0x5D294A52F;
			this.fontData['R'] = 0x24A37A52F;
			this.fontData['S'] = 0x3D087842F;
			this.fontData['T'] = 0x10842108E;
			this.fontData['U'] = 0x3D294A529;
			this.fontData['V'] = 0x18C94A529;
			this.fontData['W'] = 0x6EB5AC631;
			this.fontData['X'] = 0x294E2394A;
			this.fontData['Y'] = 0x10847294A;
			this.fontData['Z'] = 0x3C233310F;
			this.fontData['['] = 0x3C210842F;
			this.fontData['\\'] = 0x218C30C61;
			this.fontData[']'] = 0x3D084210F;
			this.fontData['^'] = 0x24C0;
			this.fontData['_'] = 0x3C0000000;
			this.fontData['`'] = 0x820;
			this.fontData['a'] = 0x7A5E878000;
			this.fontData['b'] = 0x7A52F08420;
			this.fontData['c'] = 0x7842178000;
			this.fontData['d'] = 0x7A52F42100;
			this.fontData['e'] = 0x785E978000;
			this.fontData['f'] = 0x84E109C00;
			this.fontData['g'] = 0x7A1E94BC00;
			this.fontData['h'] = 0x4A52F08420;
			this.fontData['i'] = 0x1084010000;
			this.fontData['j'] = 0x3948401000;
			this.fontData['k'] = 0x4946548000;
			this.fontData['l'] = 0x1084210000;
			this.fontData['m'] = 0x4A5EF48000;
			this.fontData['n'] = 0x4A52F00000;
			this.fontData['o'] = 0x7A52F00000;
			this.fontData['p'] = 0x84E538000;
			this.fontData['q'] = 0x421CA70000;
			this.fontData['r'] = 0x842700000;
			this.fontData['s'] = 0x390E138000;
			this.fontData['t'] = 0x3084710000;
			this.fontData['u'] = 0x394A000000;
			this.fontData['v'] = 0x114A000000;
			this.fontData['w'] = 0xFD63100000;
			this.fontData['x'] = 0x5114000000;
			this.fontData['y'] = 0x721CA50000;
			this.fontData['z'] = 0x3088600000;
			this.fontData['{'] = 0x4108211088;
			this.fontData['|'] = 0x2108421084;
			this.fontData['}'] = 0x1108841082;
			this.fontData['~'] = 0x550000;
		}
	}
}
