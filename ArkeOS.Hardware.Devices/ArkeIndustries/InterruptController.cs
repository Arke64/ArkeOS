using System.Collections.Generic;
using System.Threading;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.Devices.ArkeIndustries {
    public class InterruptController : SystemBusDevice, IInterruptController {
        private Queue<InterruptRecord> pending;
        private ManualResetEvent evt;
        private ulong[] vectors;

        public int PendingCount => this.pending.Count;

        public InterruptController() : base(ProductIds.Vendor, ProductIds.IC100, DeviceType.InterruptController) { }

		public override ulong ReadWord(ulong address) => this.vectors[address];
		public override void WriteWord(ulong address, ulong data) => this.vectors[address] = data;

		public InterruptRecord Dequeue() => this.pending.Dequeue();

		public void Enqueue(Interrupt type, ulong data1, ulong data2) {
            this.pending.Enqueue(new InterruptRecord() { Type = type, Data1 = data1, Data2 = data2, Handler = this.vectors[(int)type] });
            this.evt.Set();
        }

        public void WaitForInterrupt(int timeout) {
            while (this.PendingCount == 0) {
                this.evt.Reset();
                this.evt.WaitOne(timeout);
            }
        }

        public override void Start() {
            this.pending = new Queue<InterruptRecord>();
            this.evt = new ManualResetEvent(false);
            this.vectors = new ulong[0xFF];
        }

        public override void Stop() {
            this.evt.Set();
        }
    }
}