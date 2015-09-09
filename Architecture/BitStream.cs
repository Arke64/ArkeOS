namespace ArkeOS.Architecture {
    public class BitStream {
        public int Position { get; private set; }
        public ulong Word { get; private set; }

        public void Reset() {
            this.Reset(0);
        }

        public void Reset(ulong word) {
            this.Position = 0;
            this.Word = word;
        }

        public void Write(bool state) {
            this.Write(state ? 1UL : 0UL, 1);
        }

        public void Write(ulong bits, int size) {
            this.Word |= (bits & ((1UL << size) - 1)) << this.Position;
            this.Position += size;
        }

        public bool ReadU1() {
            return this.ReadU64(1) == 1;
        }

        public byte ReadU8(int size) {
            return (byte)this.ReadU64(size);
        }

        public ulong ReadU64(int size) {
            var result = (this.Word >> this.Position) & ((1UL << size) - 1);

            this.Position += size;

            return result;
        }
    }
}
