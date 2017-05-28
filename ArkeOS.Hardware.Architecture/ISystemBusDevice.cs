using System;

namespace ArkeOS.Hardware.Architecture {
    public interface ISystemBusDevice : IDisposable {
        ISystemBusController BusController { get; set; }
        IInterruptController InterruptController { get; set; }

        ulong Id { get; set; }
        ulong VendorId { get; }
        ulong ProductId { get; }
        DeviceType Type { get; }

        void Reset();

        void RaiseInterrupt(ulong data);

        void Copy(ulong source, ulong destination, ulong length);
        ulong[] Read(ulong source, ulong length);
        void Write(ulong destination, ulong[] data);
        ulong ReadWord(ulong address);
        void WriteWord(ulong address, ulong data);
    }
}
