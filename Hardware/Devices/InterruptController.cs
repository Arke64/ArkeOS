using System.Collections.Generic;
using System.Threading;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class InterruptController : SystemBusDevice {
        public struct Entry {
            public Interrupt Id { get; set; }
            public ulong Data1 { get; set; }
            public ulong Data2 { get; set; }
        }

        private Queue<Entry> pending;
        private ManualResetEvent evt;
        private ulong[] vectors;

        public int PendingCount => this.pending.Count;

        public InterruptController() : base(Ids.ArkeIndustries.VendorId, Ids.ArkeIndustries.Products.IC100, DeviceType.InterruptController) {

        }

        public void Enqueue(Interrupt interrupt, ulong data1, ulong data2) {
            this.pending.Enqueue(new Entry() { Id = interrupt, Data1 = data1, Data2 = data2 });
            this.evt.Set();
        }

        public Entry Dequeue() {
            return this.pending.Dequeue();
        }

        public ulong GetVector(Entry entry) {
            return this.vectors[(int)entry.Id];
        }

        public void Wait(int timeout) {
            if (this.PendingCount == 0) {
                this.evt.Reset();
                this.evt.WaitOne(timeout);
            }
        }

        public override ulong ReadWord(ulong address) {
            return this.vectors[address];
        }

        public override void WriteWord(ulong address, ulong data) {
            this.vectors[address] = data;
        }

        public override void Start() {
            this.pending = new Queue<Entry>();
            this.evt = new ManualResetEvent(false);
            this.vectors = new ulong[0xFF];
        }

        public override void Stop() {
            this.evt.Set();
        }
    }
}