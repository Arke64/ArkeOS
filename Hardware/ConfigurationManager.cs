using System.Collections.Generic;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class ConfigurationManager : IDevice {
		private ulong configuration;
		private ulong[] interruptVectors;

		public byte SystemTickInterval { get; private set; }
		public byte ProtectionMode { get; private set; }
		public bool InstructionCachingEnabled { get; private set; }

		public IReadOnlyList<ulong> InterruptVectors => this.interruptVectors;

		public ConfigurationManager() {
			this.Reset();
		}

		public override ulong ReadWord(ulong address) {
			if (address == 0) {
				return this.configuration;
			}
			else if (address >= 0x10 && address < 0xFF + 0x10) {
				return this.interruptVectors[(int)address - 0x10];
			}
			else {
				return 0;
			}
		}

		public override void WriteWord(ulong address, ulong data) {
			if (address == 0) {
				this.configuration = data;

				this.SystemTickInterval = (byte)((data & 0xFF00000000000000UL) >> 56);
				this.ProtectionMode = (byte)((data & 0x00FF000000000000UL) >> 48);
				this.InstructionCachingEnabled = (data & 0x800000000000UL) != 0;
			}
			else if (address >= 0x10 && address < 0xFF + 0x10) {
				this.interruptVectors[(int)address - 0x10] = data;
			}
		}

		public void Reset() {
			this.SystemTickInterval = 50;
			this.InstructionCachingEnabled = true;
			this.ProtectionMode = 0;

			this.interruptVectors = new ulong[0xFF];
		}
	}
}