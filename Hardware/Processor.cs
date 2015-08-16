using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		private static int MaxInstructionPipelineLength => 64;

		private class Configuration {
			public ushort SystemTickInterval { get; set; } = 50;
			public bool InstructionPipelineEnabled { get; set; } = true;
			public byte ProtectionMode { get; set; } = 0;
		}

		private struct DecodedInstruction {
			public Instruction Instruction { get; set; }
			public ulong ExpectedAddress { get; set; }
		}

		private Configuration configuration;
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Action<Instruction>[] instructionHandlers;
		private Timer systemTimer;
		private ConcurrentQueue<DecodedInstruction> decodedInstructions;
		private Dictionary<ulong, Instruction> cachedInstructions;
		private AutoResetEvent flushPipelineEvent;
		private Dictionary<ulong, ulong> jumpHistory;
		private bool flushPipeline;
		private bool supressRIPIncrement;
		private bool interruptsEnabled;
		private bool inProtectedIsr;
		private bool inIsr;
		private bool broken;
		private bool running;

		public Instruction CurrentInstruction { get; private set; }
		public RegisterManager Registers { get; private set; }

		public event EventHandler ExecutionPaused;

		public Processor(MemoryController memoryController, InterruptController interruptController) {
			this.memoryController = memoryController;
			this.interruptController = interruptController;

			this.flushPipelineEvent = new AutoResetEvent(false);
			this.systemTimer = new Timer(this.OnSystemTimerTick, null, Timeout.Infinite, Timeout.Infinite);
			this.instructionHandlers = new Action<Instruction>[InstructionDefinition.All.Max(i => i.Code) + 1];

			foreach (var i in InstructionDefinition.All)
				this.instructionHandlers[i.Code] = (Action<Instruction>)this.GetType().GetMethod("Execute" + i.CamelCaseMnemonic, BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(typeof(Action<Instruction>), this);
		}

		public void LoadStartupImage(byte[] image) {
			this.memoryController.CopyFrom(image, 0, (ulong)image.Length);
		}

		public void Start() {
			this.broken = true;
			this.running = true;

			this.Reset();

			new Task(this.MaintainInstructionPipeline, TaskCreationOptions.LongRunning).Start();

			this.SetNextInstruction();
		}

		public void Stop() {
			this.running = false;

			this.Break();
		}

		public void Break() {
			this.broken = true;
			this.systemTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Continue() {
			this.broken = false;
			this.systemTimer.Change(this.configuration.SystemTickInterval, this.configuration.SystemTickInterval);

			new Task(() => {
				while (!this.broken)
					this.Tick();
			}, TaskCreationOptions.LongRunning).Start();
		}

		public void Step() {
			this.Tick();
		}

		private void Reset() {
			this.Registers = new RegisterManager();

			this.configuration = new Configuration();
			this.decodedInstructions = new ConcurrentQueue<DecodedInstruction>();
			this.jumpHistory = new Dictionary<ulong, ulong>();
			this.cachedInstructions = new Dictionary<ulong, Instruction>();

			this.flushPipeline = false;
			this.supressRIPIncrement = false;
			this.interruptsEnabled = true;
			this.inProtectedIsr = false;
			this.inIsr = false;
		}

		private void OnSystemTimerTick(object state) {
			if (this.configuration.SystemTickInterval != 0)
				this.interruptController.Enqueue(Interrupt.SystemTimer);
		}

		private void MaintainInstructionPipeline() {
			var address = 0UL;

			while (this.running) {
				if (this.flushPipeline) {
					this.decodedInstructions = new ConcurrentQueue<DecodedInstruction>();
					this.flushPipelineEvent.Set();
					this.flushPipeline = false;

					address = this.Registers[Register.RIP];
				}

				if (this.decodedInstructions.Count < Processor.MaxInstructionPipelineLength) {
					Instruction instruction;
					ulong jumpAddress;

					if (!this.cachedInstructions.TryGetValue(address, out instruction)) {
						instruction = this.memoryController.ReadInstruction(address);
						this.cachedInstructions.Add(address, instruction);
					}

					this.decodedInstructions.Enqueue(new DecodedInstruction() { Instruction = instruction, ExpectedAddress = address });

					if (instruction.Definition.IsJump && this.jumpHistory.TryGetValue(address, out jumpAddress)) {
						address = jumpAddress;
					}
					else {
						address += instruction.Length;
					}
				}
			}
		}

		private void SetNextInstruction() {
			var address = this.Registers[Register.RIP];

			if (!this.configuration.InstructionPipelineEnabled) {
				this.CurrentInstruction = this.memoryController.ReadInstruction(address);

				return;
			}

			while (true) {
				DecodedInstruction cached;

				if (this.decodedInstructions.TryDequeue(out cached)) {
					if (cached.ExpectedAddress == address) {
						this.CurrentInstruction = cached.Instruction;

						return;
					}
					else {
						this.flushPipeline = true;
						this.flushPipelineEvent.WaitOne();
					}
				}
			}
		}

		private void Tick() {
			var startIP = this.Registers[Register.RIP];

			if (this.instructionHandlers.Length >= this.CurrentInstruction.Code && this.instructionHandlers[this.CurrentInstruction.Code] != null) {
				this.instructionHandlers[this.CurrentInstruction.Code](this.CurrentInstruction);

				if (!this.supressRIPIncrement) {
					this.Registers[Register.RIP] += this.CurrentInstruction.Length;

					if (this.CurrentInstruction.Definition.IsJump && this.jumpHistory.ContainsKey(startIP))
						this.jumpHistory.Remove(startIP);
				}
				else {
					if (this.CurrentInstruction.Definition.IsJump)
						this.jumpHistory[startIP] = this.Registers[Register.RIP];

					this.supressRIPIncrement = false;
				}
			}
			else {
				this.interruptController.Enqueue(Interrupt.InvalidInstruction);
			}

			if (this.interruptsEnabled && !this.inIsr && this.interruptController.AnyPending)
				this.EnterInterrupt(this.interruptController.Dequeue());

			this.SetNextInstruction();
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

		private void UpdateConfiguration() {
			this.configuration.SystemTickInterval = this.memoryController.ReadU16(this.Registers[Register.RCFG] + 0);
			this.configuration.InstructionPipelineEnabled = this.memoryController.ReadU16(this.Registers[Register.RCFG] + 2) != 0;
			this.configuration.ProtectionMode = this.memoryController.ReadU8(this.Registers[Register.RCFG] + 3);
		}

		private ulong GetValue(Parameter parameter) {
			var value = 0UL;

			if (parameter.Type == ParameterType.Literal) {
				value = parameter.Literal;
			}
			else if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.Registers[parameter.Register];
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				value = this.memoryController.ReadU64(parameter.Literal);
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					value = this.memoryController.ReadU64(this.Registers[parameter.Register]);
			}

			return value & this.CurrentInstruction.SizeMask;
		}

		private void SetValue(Parameter parameter, ulong value) {
			if (parameter.Type == ParameterType.Register) {
				if (this.IsRegisterWriteAllowed(parameter.Register)) {
					this.Registers[parameter.Register] = value;

					if (parameter.Register == Register.RCFG)
						this.UpdateConfiguration();
				}
			}
			else if (parameter.Type == ParameterType.LiteralAddress) {
				this.memoryController.WriteU64(parameter.Literal, value);
			}
			else if (parameter.Type == ParameterType.RegisterAddress) {
				if (this.IsRegisterReadAllowed(parameter.Register))
					this.memoryController.WriteU64(this.Registers[parameter.Register], value);
			}
		}

		private bool IsRegisterReadAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsReadProtected(register);
		private bool IsRegisterWriteAllowed(Register register) => this.inProtectedIsr || this.configuration.ProtectionMode == 0 || !this.Registers.IsWriteProtected(register);

		private void Access(Parameter a, Action<ulong> operation) => operation(this.GetValue(a));
		private void Access(Parameter a, Parameter b, Action<ulong, ulong> operation) => operation(this.GetValue(a), this.GetValue(b));
		private void Access(Parameter destination, Func<ulong> operation) => this.SetValue(destination, operation());
		private void Access(Parameter a, Parameter destination, Func<ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a)));
		private void Access(Parameter a, Parameter b, Parameter destination, Func<ulong, ulong, ulong> operation) => this.SetValue(destination, operation(this.GetValue(a), this.GetValue(b)));
	}
}