using System;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public class MemoryController {
		private byte[] memory;

		public MemoryController(ulong physicalSize) {
			this.memory = new byte[physicalSize];
		}

		public byte ReadU8(ulong address) => this.memory[address];
		public ushort ReadU16(ulong address) => BitConverter.ToUInt16(this.memory, (int)address);
		public uint ReadU32(ulong address) => BitConverter.ToUInt32(this.memory, (int)address);
		public ulong ReadU64(ulong address) => BitConverter.ToUInt64(this.memory, (int)address);
		public void WriteU8(ulong address, byte data) => this.memory[address] = data;
		public void WriteU16(ulong address, ushort data) => this.CopyFrom(BitConverter.GetBytes(data), address, 2);
		public void WriteU32(ulong address, uint data) => this.CopyFrom(BitConverter.GetBytes(data), address, 4);
		public void WriteU64(ulong address, ulong data) => this.CopyFrom(BitConverter.GetBytes(data), address, 8);

		public void Copy(ulong source, ulong destination, ulong length) {
			for (var i = 0UL; i < length; i++)
				this.memory[destination + i] = this.memory[source + i];
		}

		public void CopyFrom(byte[] source, ulong destination, ulong length) {
			for (var i = 0UL; i < length; i++)
				this.memory[destination + i] = source[i];
		}

		public void CopyTo(byte[] destination, ulong source, ulong length) {
			for (var i = 0UL; i < length; i++)
				destination[i] = this.memory[i];
		}

		public Instruction ReadInstruction(ulong address) {
			return new Instruction(this.memory, address);
		}
	}
}