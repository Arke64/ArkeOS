﻿using ArkeOS.Hardware.Architecture;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ArkeOS.Hardware.ArkeIndustries {
    public class InterruptController : SystemBusDevice, IInterruptController {
        private readonly Queue<InterruptRecord> pending;
        private readonly ManualResetEvent evt;
        private readonly ulong[] vectors;
        private bool disposed;

        public int PendingCount => this.pending.Count;

        public InterruptController() : base(ProductIds.Vendor, ProductIds.IC100, DeviceType.InterruptController) {
            this.pending = new Queue<InterruptRecord>();
            this.evt = new ManualResetEvent(false);
            this.vectors = new ulong[0x1000];
            this.disposed = false;
        }

        public override ulong ReadWord(ulong address) => this.vectors[address];
        public override void WriteWord(ulong address, ulong data) => this.vectors[address] = data;

        public InterruptRecord Dequeue() => this.pending.Dequeue();

        public void Enqueue(Interrupt type, ulong data1, ulong data2) {
            this.pending.Enqueue(new InterruptRecord() { Type = type, Data1 = data1, Data2 = data2, Handler = this.vectors[(int)type] });
            this.evt.Set();
        }

        public void WaitForInterrupt(int timeout) {
            if (this.PendingCount == 0) {
                this.evt.Reset();
                this.evt.WaitOne(timeout);
            }
        }

        public override void Stop() {
            this.evt.Set();
            this.pending.Clear();

            Array.Clear(this.vectors, 0, this.vectors.Length);
        }

        protected override void Dispose(bool disposing) {
            if (this.disposed)
                return;

            if (disposing)
                this.evt.Dispose();

            this.disposed = true;

            base.Dispose(disposing);
        }
    }
}
