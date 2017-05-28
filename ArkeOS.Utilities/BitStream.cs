namespace ArkeOS.Utilities {
    public class BitStream {
        public int Position { get; private set; }
        public ulong Word { get; private set; }

        public BitStream() : this(0) { }

        public BitStream(ulong word) {
            this.Word = word;
            this.Position = 0;
        }

        public void Advance(int size) => this.Position += size;

        public void Write(bool data) => this.Write(data ? 1U : 0U, 1);
        public void Write(byte data) => this.Write(data, 8);

        public void Write(ulong bits, int size) {
            this.Word |= (bits & ((1UL << size) - 1)) << this.Position;
            this.Position += size;
        }

        public bool ReadU1() => this.ReadU64(1) == 1;
        public byte ReadU8(int size) => (byte)this.ReadU64(size);

        public ulong ReadU64(int size) {
            var result = (this.Word >> this.Position) & ((1UL << size) - 1);

            this.Position += size;

            return result;
        }
    }
}
