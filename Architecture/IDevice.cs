namespace ArkeOS.Architecture {
	public abstract class IDevice {
		public ulong this[ulong address] {
			get {
				return this.ReadWord(address);
			}
			set {
				this.WriteWord(address, value);
			}
		}

		public abstract ulong ReadWord(ulong address);
		public abstract void WriteWord(ulong address, ulong data);
	}
}
