using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.Executable;

namespace ArkeOS.Interpreter {
	public class Interpreter {
		private Image image;
		private MemoryManager memory;
		private Dictionary<Register, ulong> registers;

		public Interpreter() {
			this.image = new Image();
			this.memory = new MemoryManager();
			this.registers = new Dictionary<Register, ulong>();

			this.registers = Enum.GetNames(typeof(Register)).Select(n => (Register)Enum.Parse(typeof(Register), n)).ToDictionary(e => e, e => 0UL);
		}

		public void Parse(byte[] data) {
			this.image.FromArray(data);

			if (this.image.Header.Magic != Header.MagicNumber) throw new InvalidProgramFormatException();
			if (!this.image.Sections.Any()) throw new InvalidProgramFormatException();

			this.image.Sections.ForEach(s => this.memory.CopyFrom(s.Data, s.Address, s.Size));

			this.registers[Register.RIP] = this.image.Header.EntryPointAddress;
			this.registers[Register.RSP] = this.image.Header.StackAddress;

			this.image = null;
		}

		public void Run() {
			while (true) {
				this.memory.Reader.BaseStream.Seek((long)this.registers[Register.RIP], SeekOrigin.Begin);

				var instruction = new Instruction(this.memory.Reader);

				if (instruction.Code == Instruction.Hlt.Code) {
					return;
				}
				else if (instruction.Code == Instruction.Nop.Code) {
					
				}
				else if (instruction.Code == Instruction.Add.Code) {
					this.registers[instruction.Rc] = this.registers[instruction.Ra] + this.registers[instruction.Rb];
				}
				else if (instruction.Code == Instruction.Jiz.Code) {

				}
				else {
					throw new InvalidInstructionException();
				}

				this.registers[Register.RIP] += instruction.Length;
			}
		}
	}
}