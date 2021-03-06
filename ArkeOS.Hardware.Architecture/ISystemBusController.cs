﻿using ArkeOS.Utilities;
using System;
using System.Collections.Generic;

namespace ArkeOS.Hardware.Architecture {
    public interface ISystemBusController : IWordStream, IDisposable {
        IProcessor Processor { get; set; }
        IInterruptController InterruptController { get; set; }

        IReadOnlyList<ISystemBusDevice> Devices { get; }

        int AddressBits { get; }
        ulong MaxAddress { get; }
        ulong MaxId { get; }

        void Start();
        void Stop();

        ulong AddDevice(ISystemBusDevice device);

        void Copy(ulong source, ulong destination, ulong length);
        ulong[] Read(ulong source, ulong length);
        void Write(ulong destination, ulong[] data);
    }
}
