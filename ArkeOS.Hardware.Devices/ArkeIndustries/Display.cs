using System.Text;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
	public class Display : SystemBusDevice {
		private const int CharacterWidth = 5;
		private const int CharacterHeight = 8;

		private ulong height;
		private ulong width;
		private ulong rows;
		private ulong columns;
		private char[] characterBuffer;
		private char[] decodeBuffer;
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
				ulong data;

				address -= 0x100000;

				fixed (char* c = this.characterBuffer)
					Encoding.UTF8.GetBytes(c + address, 1, (byte*)&data, 8);

				return data;
			}
			else {
				return 0;
			}
		}

		public override unsafe void WriteWord(ulong address, ulong data) {
			if (address < 0x100000)
				return;

			address -= 0x100000;

			fixed (char* c = this.decodeBuffer)
				Encoding.UTF8.GetChars((byte*)&data, 8, c, 8);

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
			this.fontData['O'] = 0x3D2B7B52F;
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
		}
	}
}
