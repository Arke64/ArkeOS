namespace ArkeOS.Hardware.Devices {
	public class Operand {
		private ulong value;

		public bool Dirty { get; private set; }

		public ulong Value {
			get {
				return this.value;
			}
			set {
				this.value = value;

				this.Dirty = true;
			}
		}

		public Operand() {
			this.value = 0;
			this.Dirty = false;
		}

		public void Reset(ulong value) {
			this.value = value;
			this.Dirty = false;
		}
	}
}