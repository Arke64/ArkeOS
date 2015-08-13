using System;
using System.IO;

namespace ArkeOS.Architecture {
	public static class Helpers {
		public static InstructionSize BytesToSize(int bytes) => (InstructionSize)(int)Math.Log(bytes, 2);

		public static byte SizeToBytes(InstructionSize size) => (byte)(1 << (byte)size);
		public static byte SizeToBits(InstructionSize size) => (byte)(Helpers.SizeToBytes(size) * 8);
		public static ulong SizeToMask(InstructionSize size) => (1UL << (Helpers.SizeToBits(size) - 1)) | ((1UL << (Helpers.SizeToBits(size) - 1)) - 1);

		public static ulong ParseLiteral(string value) {
			if (value.IndexOf("0x") == 0) {
				return Convert.ToUInt64(value.Substring(2), 16);
			}
			else if (value.IndexOf("0d") == 0) {
				return Convert.ToUInt64(value.Substring(2), 10);
			}
			else if (value.IndexOf("0o") == 0) {
				return Convert.ToUInt64(value.Substring(2), 8);
			}
			else if (value.IndexOf("0b") == 0) {
				return Convert.ToUInt64(value.Substring(2), 2);
			}
			else {
				return 0;
			}
		}

		public static void SizedWrite(BinaryWriter writer, ulong value, int size) {
			switch (Helpers.BytesToSize(size)) {
				case InstructionSize.OneByte: writer.Write((byte)value); break;
				case InstructionSize.TwoByte: writer.Write((ushort)value); break;
				case InstructionSize.FourByte: writer.Write((uint)value); break;
				case InstructionSize.EightByte: writer.Write(value); break;
			}
		}

		public static T ParseEnum<T>(string value) {
			return (T)Enum.Parse(typeof(T), value);
		}
	}
}
