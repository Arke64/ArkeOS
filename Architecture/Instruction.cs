using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Architecture {
	public class Instruction {
		public InstructionDefinition Definition { get; private set; }
		public byte Code { get; }
		public byte Length { get; }

		public Parameter Parameter1 { get; private set; }
		public Parameter Parameter2 { get; private set; }
		public Parameter Parameter3 { get; private set; }

		public override string ToString() {
			return this.Definition.Mnemonic + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
		}

		public Instruction(byte code, IList<Parameter> parameters) {
			this.Code = code;
			this.Length = 1;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = parameters[0];
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = parameters[1];
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = parameters[2];
				this.Length += this.Parameter3.Length;
			}
		}

		public Instruction(ulong[] memory, ulong address) {
			var instruction = memory[address++];
			var parameter1Type = (ParameterType)((instruction >> 6) & 0x07);
			var parameter2Type = (ParameterType)((instruction >> 3) & 0x07);
			var parameter3Type = (ParameterType)((instruction >> 0) & 0x07);

			this.Code = (byte)((instruction & 0xFF00000000000000UL) >> 56);
			this.Length = 1;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = this.CreateParameter(parameter1Type, instruction, 0, memory, ref address);
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = this.CreateParameter(parameter2Type, instruction, 1, memory, ref address);
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = this.CreateParameter(parameter3Type, instruction, 2, memory, ref address);
				this.Length += this.Parameter3.Length;
			}
		}

		public void Encode(BinaryWriter writer) {
			var value = (ulong)this.Code << 56;

			value |= (ulong)(this.Parameter1?.Type ?? 0) << 6;
			value |= (ulong)(this.Parameter2?.Type ?? 0) << 3;
			value |= (ulong)(this.Parameter3?.Type ?? 0) << 0;

			value |= (this.Parameter1?.Type == ParameterType.Register || this.Parameter1?.Type == ParameterType.RegisterAddress ? (ulong)this.Parameter1.Register : 0) << 40;
			value |= (this.Parameter2?.Type == ParameterType.Register || this.Parameter2?.Type == ParameterType.RegisterAddress ? (ulong)this.Parameter2.Register : 0) << 32;
			value |= (this.Parameter3?.Type == ParameterType.Register || this.Parameter3?.Type == ParameterType.RegisterAddress ? (ulong)this.Parameter3.Register : 0) << 24;

			writer.Write(value);

			this.EncodeParameter(writer, this.Parameter1);
			this.EncodeParameter(writer, this.Parameter2);
			this.EncodeParameter(writer, this.Parameter3);
		}

		private void EncodeParameter(BinaryWriter writer, Parameter parameter) {
			if (parameter == null)
				return;

			switch (parameter.Type) {
				case ParameterType.LiteralAddress:
				case ParameterType.Literal:
					writer.Write(parameter.Literal);

					break;

				case ParameterType.CalculatedAddress:
				case ParameterType.Calculated:
					ulong format = 0;

					this.EncodeCalculatedFormat(parameter.Base, 0, ref format);
					this.EncodeCalculatedFormat(parameter.Index, 1, ref format);
					this.EncodeCalculatedFormat(parameter.Scale, 2, ref format);
					this.EncodeCalculatedFormat(parameter.Offset, 3, ref format);

					writer.Write((ulong)format);

					this.EncodeParameter(writer, parameter.Base.Parameter);
					this.EncodeParameter(writer, parameter.Index.Parameter);
					this.EncodeParameter(writer, parameter.Scale?.Parameter);
					this.EncodeParameter(writer, parameter.Offset?.Parameter);

					break;
			}
		}

		private void EncodeCalculatedFormat(Parameter.Calculated operand, int parameter, ref ulong format) {
			if (operand == null)
				return;

			format |= (operand.Parameter.Type == ParameterType.Register || operand.Parameter.Type == ParameterType.RegisterAddress ? (ulong)operand.Parameter.Register : 0) << (40 - 8 * parameter);
			format |= (((ulong)operand.Parameter.Type << 1) + (operand.IsPositive ? 1UL : 0UL)) << (60 - 4 * parameter);
		}

		private Parameter CreateParameter(ParameterType type, ulong instruction, int parameter, ulong[] memory, ref ulong address) {
			switch (type) {
				default: return null;
				case ParameterType.RegisterAddress: return Parameter.CreateRegister(true, (Register)((instruction >> (40 - 8 * parameter)) & 0xFF));
				case ParameterType.Register: return Parameter.CreateRegister(false, (Register)((instruction >> (40 - 8 * parameter)) & 0xFF));
				case ParameterType.LiteralAddress: return Parameter.CreateLiteral(true, memory[address++]);
				case ParameterType.Literal: return Parameter.CreateLiteral(false, memory[address++]);
				case ParameterType.StackAddress: return Parameter.CreateStack(true);
				case ParameterType.Stack: return Parameter.CreateStack(false);
				case ParameterType.CalculatedAddress:
				case ParameterType.Calculated:
					var calculated = memory[address++];

					var @base = this.CreateCalculatedOperand(calculated, 0, memory, ref address);
					var index = this.CreateCalculatedOperand(calculated, 1, memory, ref address);
					var scale = this.CreateCalculatedOperand(calculated, 2, memory, ref address);
					var offset = this.CreateCalculatedOperand(calculated, 3, memory, ref address);

					return Parameter.CreateCalculated(type == ParameterType.CalculatedAddress, @base, index, scale, offset);
			}
		}

		private Parameter.Calculated CreateCalculatedOperand(ulong calculated, int parameter, ulong[] memory, ref ulong address) {
			var format = calculated >> (60 - 4 * parameter);
			var type = (ParameterType)((format & 0x0E) >> 1);

			if (type == ParameterType.Calculated || type == ParameterType.CalculatedAddress)
				return null;

			return new Parameter.Calculated(this.CreateParameter(type, calculated, parameter, memory, ref address), (format & 0x01) != 0);
		}
	}
}