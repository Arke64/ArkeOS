using System;
using System.Collections.Generic;
using System.Threading;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class InterruptController {
		private Queue<Tuple<Interrupt, ulong>> pending;
		private ManualResetEvent evt;

		public bool AnyPending => this.pending.Count > 0;

		public InterruptController() {
			this.pending = new Queue<Tuple<Interrupt, ulong>>();
			this.evt = new ManualResetEvent(false);
		}

		public void Enqueue(Interrupt interrupt, ulong data) {
			this.pending.Enqueue(Tuple.Create(interrupt, data));
			this.evt.Set();
		}

		public Tuple<Interrupt, ulong> Dequeue() {
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