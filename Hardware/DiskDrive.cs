using System;
using System.IO;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class DiskDrive : BusDevice {
        private Stream stream;
        private byte[] buffer;

        public override ulong VendorId => 1;
        public override ulong ProductId => 4;

        public DiskDrive(ulong physicalSize, Stream stream) {
            this.stream = stream;
            this.stream.SetLength((long)physicalSize * 8);

            this.buffer = new byte[8];
        }

        public override ulong ReadWord(ulong address) {
            this.stream.Seek((long)address * 8, SeekOrigin.Begin);
            this.stream.Read(this.buffer, 0, 8);

            return BitConverter.ToUInt64(this.buffer, 0);
        }

        public override void WriteWord(ulong address, ulong data) {
            var buffer = BitConverter.GetBytes(data);

            this.stream.Seek((long)address * 8, SeekOrigin.Begin);
            this.stream.Write(buffer, 0, 8);
        }

        public override ulong[] Read(ulong source, ulong length) {
            var buffer = new byte[length * 8];
            var result = new ulong[length];

            this.stream.Seek((long)source * 8, SeekOrigin.Begin);
            this.stream.Read(buffer, 0, buffer.Length);

            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);

            return result;
        }

        public override void Write(ulong destination, ulong[] data) {
            var buffer = new byte[data.Length * 8];

            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);

            this.stream.Seek((long)destination * 8, SeekOrigin.Begin);
            this.stream.Read(buffer, 0, buffer.Length);
        }
    }
}