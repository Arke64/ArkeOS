using System;
using System.Collections.Generic;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public class RegisterManager {
		private ulong[] registers;

		public IReadOnlyList<Register> ReadProtectedRegisters => new List<Register> { Register.RSIP, Register.RIDT, Register.RMDT, Register.RTDT, Register.RCFG };
		public IReadOnlyList<Register> WriteProtectedRegisters => new List<Register> { Register.RSIP, Register.RIDT, Register.RMDT, Register.RTDT, Register.RCFG, Register.RO, Register.RF };

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