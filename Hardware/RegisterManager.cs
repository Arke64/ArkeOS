using System;
using System.Linq;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class RegisterManager {
		private ulong[] registers;

		public bool IsReadProtected(Register register) => register == Register.RSIP;
		public bool IsWriteProtected(Register register) => register == Register.RSIP || register == Register.RO || register == Register.RF;

		public RegisterManager() {
			var values = Enum.GetValues(typeof(Register)).Cast<Register>();

			this.registers = new ulong[values.Max(v => (ulong)v) + 1];

			this[Register.RF] = ulong.MaxValue;
		}

		public ulong this[Register register] {
			get {
				return this.registers[(ulong)register];
			}
			set {
				this.registers[(ulong)register] = value;
			}
		}
	}
}