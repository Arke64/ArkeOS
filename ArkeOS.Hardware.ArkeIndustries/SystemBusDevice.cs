using System;
using ArkeOS.Hardware.Architecture;

namespace ArkeOS.Hardware.ArkeIndustries {
	public abstract class SystemBusDevice : ISystemBusDevice {
		private bool disposed;

		public ISystemBusController BusController { get; set; }
		public IInterruptController InterruptController { get; set; }
		public ulong Id { get; set; }

		public ulong VendorId { get; }
		public ulong ProductId { get; }
		public DeviceType Type { get; }

		protected SystemBusDevice(ulong vendorId, ulong productId, DeviceType type) {
			this.disposed = false;
			this.VendorId = vendorId;
			this.ProductId = productId;
			this.Type = type;
		}

		public virtual void Reset() {

		}

		public void RaiseInterrupt(ulong data) => this.InterruptController.Enqueue(Interrupt.DeviceWaiting, this.Id, data);
		public void RaiseInterrupt(Interrupt type, ulong data1, ulong data2) => this.InterruptController.Enqueue(type, data1, data2);

		public virtual ulong ReadWord(ulong address) {
			return 0;
		}

		public virtual void WriteWord(ulong address, ulong data) {

		}

		public virtual ulong[] Read(ulong source, ulong length) {
			var buffer = new ulong[length];

			for (var i = 0UL; i < length; i++)
				buffer[i] = this.ReadWord(source + i);

			return buffer;
		}

		public virtual void Write(ulong destination, ulong[] data) {
			for (var i = 0UL; i < (ulong)data.Length; i++)
				this.WriteWord(destination + i, data[i]);
		}

		public virtual void Copy(ulong source, ulong destination, ulong length) {
			this.Write(destination, this.Read(source, length));
		}

		protected virtual void Dispose(bool disposing) {
			if (this.disposed)
				return;

			this.disposed = true;
		}

		public void Dispose() {
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}
	}
}
