namespace ArkeOS.Hardware {
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

		public Operand(ulong initialValue) {
			this.value = initialValue;
			this.Dirty = false;
		}
	}
}