using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.Executable;
using ArkeOS.ISA;

namespace ArkeOS.Interpreter {
	public partial class Interpreter {
		private MemoryManager memory;
		private Dictionary<Register, ulong> registers;
		private Dictionary<InstructionDefinition, Action<Instruction>> instructionHandlers;
		private InstructionSize currentSize;
		private bool supressRIPIncrement;
		private bool running;
		private bool inProtectedIsr;
		private Queue<Interrupt> pendingInterrupts;

		public Interpreter() {
			this.memory = new MemoryManager();
			this.pendingInterrupts = new Queue<Interrupt>();

			this.instructionHandlers = InstructionDefinition.All.ToDictionary(i => i, i => (Action<Instruction>)Delegate.CreateDelegate(typeof(Action<Instruction>), this, i.Mnemonic, true));
			this.registers = Enum.GetValues(typeof(Register)).Cast<Register>().ToDictionary(e => e, e => 0UL);

			this.registers[Register.RO] = 0;
			this.registers[Register.RF] = ulong.MaxValue;
			this.running = true;
			this.supressRIPIncrement = false;
			this.inProtectedIsr = false;
		}

		public void Load(byte[] data) {
			var image = new Image(data);

			if (image.Header.Magic != Header.MagicNumber) throw new InvalidProgramFormatException();
			if (!image.Sections.Any()) throw new InvalidProgramFormatException();

			image.Sections.ForEach(s => this.memory.CopyFrom(s.Data, s.Address, s.Size));

			this.registers[Register.RIP] = image.Header.EntryPointAddress;
			this.registers[Register.RSP] = image.Header.StackAddress;
		}

		public void Run() {
			while (this.running) {
				if (this.pendingInterrupts.Any())
					this.EnterInterrupt(this.pendingInterrupts.Dequeue());

				this.memory.Reader.BaseStream.Seek((long)this.registers[Register.RIP], SeekOrigin.Begin);

				var instruction = new Instruction(this.memory.Reader);

				this.currentSize = instruction.Size;

				if (this.instructionHandlers.ContainsKey(instruction.Definition)) {
					this.instructionHandlers[instruction.Definition](instruction);

					if (!this.supressRIPIncrement)
						this.registers[Register.RIP] += instruction.Length;

					this.supressRIPIncrement = false;
				}
				else {
					this.pendingInterrupts.Enqueue(Interrupt.InvalidInstruction);
				}
			}
		}

		private void EnterInterrupt(Interrupt id) {
			var table = this.memory.ReadU64(this.registers[Register.RIDT]);
			var isr = this.memory.ReadU64(table + (ulong)id * 8);

			this.registers[Register.RSIP] = this.registers[Register.RIP];
			this.registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
		}

		private void UpdateFlags(ulong value) {
			this.registers[Register.RZ] = value == 0 ? ulong.MaxValue : 0;
			this.registers[Register.RS] = (value & (ulong)(1 << (Instruction.SizeToBits(this.currentSize) - 1))) != 0 ? ulong.MaxValue : 0;
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			switch (parameter.Type) {
				case ParameterType.Literal:
					value = parameter.Literal;
					break;

				case ParameterType.Register:
					if (this.IsRegisterAccessAllowed(parameter.Register))
						value = this.registers[parameter.Register];

					break;

				case ParameterType.LiteralAddress:
					value = this.memory.ReadU64(parameter.Literal);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterAccessAllowed(parameter.Register))
						value = this.memory.ReadU64(this.registers[parameter.Register]);

					break;

				default:
					throw new ArgumentException(nameof(parameter));
			}

			unchecked {
				return value & Instruction.SizeToMask(this.currentSize);
			}
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.UpdateFlags(value);

			var orig = value;

			value &= Instruction.SizeToMask(this.currentSize);

			if (value != orig)
				this.registers[Register.RC] = ulong.MaxValue;

			switch (parameter.Type) {
				case ParameterType.Literal:
					break;

				case ParameterType.Register:
					if (this.IsRegisterAccessAllowed(parameter.Register))
						this.registers[parameter.Register] = value;

					break;

				case ParameterType.LiteralAddress:
					this.memory.WriteU64(parameter.Literal, value);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterAccessAllowed(parameter.Register))
						this.memory.WriteU64(this.registers[parameter.Register], value);

					break;

				default:
					throw new ArgumentException(nameof(parameter));
			}
		}

		private bool IsRegisterAccessAllowed(Register register) {
			return this.inProtectedIsr || this.registers[Register.RMDE] == 0 || (register != Register.RO && register != Register.RF && register != Register.RMDE && register != Register.RSIP && register != Register.RIDT && register != Register.RMDT && register != Register.RTDT);
		}

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}