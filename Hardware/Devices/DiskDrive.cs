using System;
using System.IO;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class DiskDrive : SystemBusDevice {
        private Stream stream;
        private byte[] buffer;
        private ulong length;

        public DiskDrive(Stream stream) : base(1, 5, DeviceType.DiskDrive) {
            this.stream = stream;
            this.length = (ulong)stream.Length * 8UL;

            this.buffer = new byte[8];
        }

        public override ulong ReadWord(ulong address) {
            if (address >= this.length)
                return 0;

            this.stream.Seek((long)address * 8, SeekOrigin.Begin);
            this.stream.Read(this.buffer, 0, 8);

            return BitConverter.ToUInt64(this.buffer, 0);
        }

        public override void WriteWord(ulong address, ulong data) {
            if (address >= this.length)
                return;

            var buffer = BitConverter.GetBytes(data);

            this.stream.Seek((long)address * 8, SeekOrigin.Begin);
            this.stream.Write(buffer, 0, 8);
        }

        public override ulong[] Read(ulong source, ulong length) {
            var buffer = new byte[length * 8];

            if (source < this.length) {
                this.stream.Seek((long)source * 8, SeekOrigin.Begin);
                this.stream.Read(buffer, 0, buffer.Length);
            }

            return Helpers.ConvertArray(buffer);
        }

        public override void Write(ulong destination, ulong[] data) {
            if (destination >= this.length)
                return;

            var buffer = Helpers.ConvertArray(data);

            this.stream.Seek((long)destination * 8, SeekOrigin.Begin);
            this.stream.Read(buffer, 0, buffer.Length);
        }

        public override void Start() {

        }

        public override void Stop() {
            this.stream.Flush();
            this.stream.Dispose();
        }
    }
}