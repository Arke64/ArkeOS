using System;
using System.Collections.Generic;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public class RegisterManager {
		private Dictionary<Register, ulong> registers;

		public IReadOnlyList<Register> ReadProtectedRegisters => new List<Register> { Register.RSIP, Register.RIDT, Register.RMDT, Register.RTDT };
		public IReadOnlyList<Register> WriteProtectedRegisters => new List<Register> { Register.RSIP, Register.RIDT, Register.RMDT, Register.RTDT, Register.RMDE, Register.RO, Register.RF };

		public RegisterManager() {
			this.registers = Enum.GetValues(typeof(Register)).Cast<Register>().ToDictionary(e => e, e => 0UL);

			this.registers[Register.RF] = ulong.MaxValue;
		}

		public ulong this[Register register] {
			get {
				return this.registers[register];
			}
			set {
				this.registers[register] = value;
			}
		}
	}
}
