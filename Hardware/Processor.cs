using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private MemoryManager memory;
		private RegisterManager registers;
		private Dictionary<InstructionDefinition, Action<Instruction>> instructionHandlers;
		private Instruction currentInstruction;
		private bool supressRIPIncrement;
		private bool running;
		private bool inProtectedIsr;
		private Queue<Interrupt> pendingInterrupts;

		public Processor(MemoryManager memory) {
			this.memory = memory;
			this.registers = new RegisterManager();
			this.pendingInterrupts = new Queue<Interrupt>();

			this.instructionHandlers = InstructionDefinition.All.ToDictionary(i => i, i => (Action<Instruction>)Delegate.CreateDelegate(typeof(Action<Instruction>), this, i.Mnemonic, true));
			
			this.supressRIPIncrement = false;
			this.inProtectedIsr = false;
		}

		public void LoadBootImage(Stream image) {
			var buffer = new byte[image.Length];

			image.Read(buffer, 0, (int)image.Length);

			this.memory.CopyFrom(buffer, 0, (ulong)image.Length);
		}

		public void Run() {
			this.running = true;

			while (this.running) {
				if (this.pendingInterrupts.Any())
					this.EnterInterrupt(this.pendingInterrupts.Dequeue());

				this.currentInstruction = this.memory.ReadInstruction(this.registers[Register.RIP]);

				if (this.instructionHandlers.ContainsKey(this.currentInstruction.Definition)) {
					this.instructionHandlers[this.currentInstruction.Definition](this.currentInstruction);

					if (!this.supressRIPIncrement)
						this.registers[Register.RIP] += this.currentInstruction.Length;

					this.supressRIPIncrement = false;
				}
				else {
					this.pendingInterrupts.Enqueue(Interrupt.InvalidInstruction);
				}
			}
		}

		public void Stop() {
			this.running = false;
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
			this.registers[Register.RS] = (value & (ulong)(1 << (this.currentInstruction.SizeInBits - 1))) != 0 ? ulong.MaxValue : 0;
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			switch (parameter.Type) {
				case ParameterType.Literal:
					value = parameter.Literal;
					break;

				case ParameterType.Register:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.registers[parameter.Register];

					break;

				case ParameterType.LiteralAddress:
					value = this.memory.ReadU64(parameter.Literal);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.memory.ReadU64(this.registers[parameter.Register]);

					break;
			}

			return value & this.currentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.UpdateFlags(value);

			var orig = value;

			value &= this.currentInstruction.SizeMask;

			if (value != orig)
				this.registers[Register.RC] = ulong.MaxValue;

			switch (parameter.Type) {
				case ParameterType.Register:
					if (this.IsRegisterWriteAllowed(parameter.Register))
						this.registers[parameter.Register] = value;

					break;

				case ParameterType.LiteralAddress:
					this.memory.WriteU64(parameter.Literal, value);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						this.memory.WriteU64(this.registers[parameter.Register], value);

					break;
			}
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.registers[Register.RMDE] == 0 || !this.registers.ReadProtectedRegisters.Contains(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.registers[Register.RMDE] == 0 || !this.registers.WriteProtectedRegisters.Contains(register);

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}