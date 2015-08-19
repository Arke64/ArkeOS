using System;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public class MemoryController {
		private ulong[] memory;

		public MemoryController(ulong physicalSize) {
			this.memory = new ulong[physicalSize];
		}

		public ulong ReadWord(ulong address) => this.memory[address];
		public void WriteWord(ulong address, ulong data) => this.memory[address] = data;

		public void BlockCopy(ulong source, ulong destination, ulong length) => Buffer.BlockCopy(this.memory, (int)source, this.memory, (int)destination, (int)length * 8);
		public void CopyFrom(ulong[] source, ulong destination, ulong length) => Buffer.BlockCopy(source, 0, this.memory, (int)destination, (int)length * 8);
		public void CopyTo(ulong[] destination, ulong source, ulong length) => Buffer.BlockCopy(this.memory, (int)source, destination, 0, (int)length * 8);

		public Instruction ReadInstruction(ulong address) => new Instruction(this.memory, address);
	}
}