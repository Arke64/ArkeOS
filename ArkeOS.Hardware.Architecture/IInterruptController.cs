namespace ArkeOS.Hardware.Architecture {
    public interface IInterruptController {
        int PendingCount { get; }

        void Enqueue(Interrupt type, ulong data1, ulong data2);
        InterruptRecord Dequeue();
        void WaitForInterrupt(int timeout);
    }
}
