using System.Collections.Generic;
using System.Threading;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public class InterruptController {
		private Queue<Interrupt> pending;
		private ManualResetEvent evt;

		public bool AnyPending => this.pending.Count > 0;

		public InterruptController() {
			this.pending = new Queue<Interrupt>();
			this.evt = new ManualResetEvent(false);
		}

		public void Enqueue(Interrupt interrupt) {
			this.pending.Enqueue(interrupt);
			this.evt.Set();
		}

		public Interrupt Dequeue() {
			return this.pending.Dequeue();
		}

		public void Wait(int timeout) {
			if (!this.AnyPending) {
				this.evt.Reset();
				this.evt.WaitOne(timeout);
			}
		}
	}
}