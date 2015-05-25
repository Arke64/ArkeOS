using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Dictionary<InstructionDefinition, Action<Instruction>> instructionHandlers;
		private Instruction currentInstruction;
		private bool supressRIPIncrement;
		private bool inProtectedIsr;
		private bool running;
		private bool paused;
		private AutoResetEvent pauseEvent;

		public RegisterManager Registers { get; }

		public Processor(MemoryController memoryController, InterruptController interruptController) {
			this.memoryController = memoryController;
			this.interruptController = interruptController;

			this.instructionHandlers = InstructionDefinition.All.ToDictionary(i => i, i => (Action<Instruction>)Delegate.CreateDelegate(typeof(Action<Instruction>), this, i.Mnemonic, true));
			
			this.supressRIPIncrement = false;
			this.inProtectedIsr = false;
			this.paused = false;
			this.pauseEvent = new AutoResetEvent(false);

			this.Registers = new RegisterManager();
		}

		public void LoadBootImage(Stream image) {
			var buffer = new byte[image.Length];

			image.Read(buffer, 0, (int)image.Length);

			this.memoryController.CopyFrom(buffer, 0, (ulong)image.Length);
		}

		public void Start() {
			this.running = true;

			Task.Run(() => {
				while (this.running) {
					if (this.interruptController.AnyPending)
						this.EnterInterrupt(this.interruptController.Dequeue());

					this.currentInstruction = this.memoryController.ReadInstruction(this.Registers[Register.RIP]);

					if (this.instructionHandlers.ContainsKey(this.currentInstruction.Definition)) {
						this.instructionHandlers[this.currentInstruction.Definition](this.currentInstruction);

						if (!this.supressRIPIncrement)
							this.Registers[Register.RIP] += this.currentInstruction.Length;

						this.supressRIPIncrement = false;
					}
					else {
						this.interruptController.Enqueue(Interrupt.InvalidInstruction);
					}

					if (this.paused)
						this.pauseEvent.WaitOne();
				}
			});
		}

		public void Stop() {
			this.running = false;

			this.Unpause();
		}

		public void Pause() {
			this.paused = true;
		}

		public void Unpause() {
			this.paused = false;
			this.pauseEvent.Set();
		}

		private void EnterInterrupt(Interrupt id) {
			var table = this.memoryController.ReadU64(this.Registers[Register.RIDT]);
			var isr = this.memoryController.ReadU64(table + (ulong)id * 8);

			this.Registers[Register.RSIP] = this.Registers[Register.RIP];
			this.Registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
		}

		private void UpdateFlags(ulong value) {
			this.Registers[Register.RZ] = value == 0 ? ulong.MaxValue : 0;
			this.Registers[Register.RS] = (value & (ulong)(1 << (this.currentInstruction.SizeInBits - 1))) != 0 ? ulong.MaxValue : 0;
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			switch (parameter.Type) {
				case ParameterType.Literal:
					value = parameter.Literal;
					break;

				case ParameterType.Register:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.Registers[parameter.Register];

					break;

				case ParameterType.LiteralAddress:
					value = this.memoryController.ReadU64(parameter.Literal);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						value = this.memoryController.ReadU64(this.Registers[parameter.Register]);

					break;
			}

			return value & this.currentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.UpdateFlags(value);

			var orig = value;

			value &= this.currentInstruction.SizeMask;

			if (value != orig)
				this.Registers[Register.RC] = ulong.MaxValue;

			switch (parameter.Type) {
				case ParameterType.Register:
					if (this.IsRegisterWriteAllowed(parameter.Register))
						this.Registers[parameter.Register] = value;

					break;

				case ParameterType.LiteralAddress:
					this.memoryController.WriteU64(parameter.Literal, value);

					break;

				case ParameterType.RegisterAddress:
					if (this.IsRegisterReadAllowed(parameter.Register))
						this.memoryController.WriteU64(this.Registers[parameter.Register], value);

					break;
			}
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.Registers[Register.RMDE] == 0 || !this.Registers.ReadProtectedRegisters.Contains(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.Registers[Register.RMDE] == 0 || !this.Registers.WriteProtectedRegisters.Contains(register);

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}