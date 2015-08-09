using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.ISA;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Action<Instruction>[] instructionHandlers;
		private AutoResetEvent breakEvent;
		private Timer systemTimer;
		private bool supressRIPIncrement;
		private bool inProtectedIsr;
		private bool inIsr;
		private bool running;
		private bool broken;

		public Instruction CurrentInstruction { get; private set; }
		public RegisterManager Registers { get; }

		public event EventHandler ExecutionPaused;

		public Processor(MemoryController memoryController, InterruptController interruptController) {
			this.memoryController = memoryController;
			this.interruptController = interruptController;

			this.breakEvent = new AutoResetEvent(false);
			this.systemTimer = new Timer(s => this.interruptController.Enqueue(Interrupt.SystemTimer), null, Timeout.Infinite, Timeout.Infinite);
			this.instructionHandlers = new Action<Instruction>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Instruction>)this.GetType().GetMethod("Execute" + i.CamelCaseMnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Instruction>), this);

			this.Registers = new RegisterManager();
		}

		public void LoadImage(ulong address, Stream image) {
			var buffer = new byte[image.Length];

			image.Read(buffer, 0, (int)image.Length);

			this.memoryController.CopyFrom(buffer, address, (ulong)image.Length);
		}

		public void Start() {
			this.supressRIPIncrement = false;
			this.inProtectedIsr = false;
			this.running = true;
			this.broken = true;

			Task.Run(() => {
				while (this.running)
					this.Tick();
			});
		}

		public void Stop() {
			this.running = false;
			this.broken = false;
			this.breakEvent.Set();
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Break() {
			this.broken = true;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.broken = false;
			this.breakEvent.Set();
			this.systemTimer.Change(50, 50);
		}

		public void Step() {
			this.breakEvent.Set();
		}

		private void Tick() {
			if (!this.inIsr && this.interruptController.AnyPending)
				this.EnterInterrupt(this.interruptController.Dequeue());

			this.CurrentInstruction = this.memoryController.ReadInstruction(this.Registers[Register.RIP]);

			if (this.broken)
				this.breakEvent.WaitOne();

			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.instructionHandlers[this.CurrentInstruction.Code](this.CurrentInstruction);

				if (!this.supressRIPIncrement)
					this.Registers[Register.RIP] += this.CurrentInstruction.Length;

				this.supressRIPIncrement = false;
			}
			else {
				this.interruptController.Enqueue(Interrupt.InvalidInstruction);
			}
		}

		private void EnterInterrupt(Interrupt id) {
			if (this.Registers[Register.RIDT] == 0)
				return;

			var isr = this.memoryController.ReadU64(this.Registers[Register.RIDT] + (ulong)id * 8);

			if (isr == 0)
				return;

			this.Registers[Register.RSIP] = this.Registers[Register.RIP];
			this.Registers[Register.RIP] = isr;

			this.inProtectedIsr = (byte)id <= 0x07;
			this.inIsr = true;
		}

		private void UpdateFlags(ulong value) {
			this.Registers[Register.RZ] = value == 0 ? ulong.MaxValue : 0;
			this.Registers[Register.RS] = (value & (ulong)(1 << (this.CurrentInstruction.SizeInBits - 1))) != 0 ? ulong.MaxValue : 0;
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

			return value & this.CurrentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			this.UpdateFlags(value);

			var orig = value;

			value &= this.CurrentInstruction.SizeMask;

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

		private void Push(InstructionSize size, ulong value) {
			this.Registers[Register.RSP] -= Instruction.SizeToBytes(size);

			switch (size) {
				case InstructionSize.OneByte: this.memoryController.WriteU8(this.Registers[Register.RSP], (byte)value); break;
				case InstructionSize.TwoByte: this.memoryController.WriteU16(this.Registers[Register.RSP], (ushort)value); break;
				case InstructionSize.FourByte: this.memoryController.WriteU32(this.Registers[Register.RSP], (uint)value); break;
				case InstructionSize.EightByte: this.memoryController.WriteU64(this.Registers[Register.RSP], value); break;
			}
		}

		private ulong Pop(InstructionSize size) {
			var value = 0UL;

			switch (size) {
				case InstructionSize.OneByte: value = this.memoryController.ReadU8(this.Registers[Register.RSP]); break;
				case InstructionSize.TwoByte: value = this.memoryController.ReadU16(this.Registers[Register.RSP]); break;
				case InstructionSize.FourByte: value = this.memoryController.ReadU32(this.Registers[Register.RSP]); break;
				case InstructionSize.EightByte: value = this.memoryController.ReadU64(this.Registers[Register.RSP]); break;
			}

			this.Registers[Register.RSP] += Instruction.SizeToBytes(size);

			return value;
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