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
		private InstructionSize currentSize;

		public Interpreter() {
			this.image = new Image();
			this.memory = new MemoryManager();
			this.registers = new Dictionary<Register, ulong>();

			this.registers = Enum.GetNames(typeof(Register)).Select(n => (Register)Enum.Parse(typeof(Register), n)).ToDictionary(e => e, e => 0UL);

			this.registers[Register.RO] = 0;
			this.registers[Register.RF] = ulong.MaxValue;
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

				this.currentSize = instruction.Size;

				if (instruction.Code == InstructionDefinition.Hlt.Code) {
					return;
				}
				else if (instruction.Code == InstructionDefinition.Nop.Code) {
					
				}
				else if (instruction.Code == InstructionDefinition.Add.Code) {
					var aa = this.GetValue(instruction.A);
					var bb = this.GetValue(instruction.B);

					if (this.currentSize == InstructionSize.EightByte && ulong.MaxValue - aa < bb) {
						unchecked {
							this.SetValue(instruction.C, aa + bb);
						}

						this.registers[Register.RC] = ulong.MaxValue;
					}
					else {
						this.Access(instruction.A, instruction.B, instruction.C, (a, b) => a + b);
					}
				}
				else if (instruction.Code == InstructionDefinition.Jiz.Code) {

				}
				else {
					throw new InvalidInstructionException();
				}

				this.registers[Register.RIP] += instruction.Length;
			}
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			switch (parameter.Type) {
				case ParameterType.Literal: value = parameter.Literal; break;
				case ParameterType.Register: value = this.registers[parameter.Register]; break;
				case ParameterType.LiteralAddress: value = this.memory.ReadU64(parameter.Literal); break;
				case ParameterType.RegisterAddress: value = this.memory.ReadU64(this.registers[parameter.Register]); break;
				default: throw new ArgumentException(nameof(parameter));
			}

			switch (this.currentSize) {
				case InstructionSize.OneByte: value = (byte)value; break;
				case InstructionSize.TwoByte: value = (ushort)value; break;
				case InstructionSize.FourByte: value = (uint)value; break;
			}

			return value;
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.registers[Register.RZ] = value == 0 ? ulong.MaxValue : 0;
			this.registers[Register.RS] = (value & (ulong)(1 << (2 ^ ((byte)this.currentSize * 8 - 1)))) != 0 ? ulong.MaxValue : 0;
			this.registers[Register.RC] = 0;

			switch (this.currentSize) {
				case InstructionSize.OneByte:
					if (value > byte.MaxValue) {
						value &= 0xFF;

						this.registers[Register.RC] = ulong.MaxValue;
					}

					break;

				case InstructionSize.TwoByte:
					if (value > ushort.MaxValue) {
						value &= 0xFFFF;

						this.registers[Register.RC] = ulong.MaxValue;
					}

					break;

				case InstructionSize.FourByte:
					if (value > uint.MaxValue) {
						value &= 0xFFFFFFFF;

						this.registers[Register.RC] = ulong.MaxValue;
					}

					break;
			}

			switch (parameter.Type) {
				case ParameterType.Literal: break;
				case ParameterType.Register: this.registers[parameter.Register] = value; break;
				case ParameterType.LiteralAddress: this.memory.WriteU64(parameter.Literal, value); break;
				case ParameterType.RegisterAddress: this.memory.WriteU64(this.registers[parameter.Register], value); break;
				default: throw new ArgumentException(nameof(parameter));
			}
		}

		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}