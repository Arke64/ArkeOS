namespace ArkeOS.Architecture {
    public interface IWordStream {
        ulong ReadWord(ulong address);
        void WriteWord(ulong address, ulong data);
    }
}
