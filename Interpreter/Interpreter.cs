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

			this.registers[Register.R0] = 1;
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

				if (instruction.Code == InstructionDefinition.Hlt.Code) {
					return;
				}
				else if (instruction.Code == InstructionDefinition.Nop.Code) {
					
				}
				else if (instruction.Code == InstructionDefinition.Add.Code) {
					this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a + b);
				}
				else if (instruction.Code == InstructionDefinition.Jiz.Code) {

				}
				else {
					throw new InvalidInstructionException();
				}

				this.registers[Register.RIP] += instruction.Length;
			}
		}

		private ulong GetValue(Parameter p) {
			switch (p.Type) {
				case ParameterType.Literal: return p.Literal;
				case ParameterType.Register: return this.registers[p.Register];
				case ParameterType.LiteralAddress: return this.memory.ReadU64(p.Literal);
				case ParameterType.RegisterAddress: return this.memory.ReadU64(this.registers[p.Register]);
				default: throw new ArgumentException(nameof(p));
			}
		}

		private void SetValue(Parameter p, ulong value) {
			switch (p.Type) {
				case ParameterType.Literal: break;
				case ParameterType.Register: this.registers[p.Register] = value; break;
				case ParameterType.LiteralAddress: this.memory.WriteU64(p.Literal, value); break;
				case ParameterType.RegisterAddress: this.memory.WriteU64(this.registers[p.Register], value); break;
				default: throw new ArgumentException(nameof(p));
			}
		}

		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}