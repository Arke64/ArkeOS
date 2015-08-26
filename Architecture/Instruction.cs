using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Architecture {
    public class Instruction {
        public byte Code { get; }
        public byte Length { get; }
        public InstructionDefinition Definition { get; }

        public Parameter ConditionalParameter { get; }
        public bool ConditionalZero { get; }

        public Parameter Parameter1 { get; }
        public Parameter Parameter2 { get; }
        public Parameter Parameter3 { get; }

        public override string ToString() {
            var str = this.Definition.Mnemonic;

            if (this.ConditionalParameter != null) {
                str += this.ConditionalZero ? ":Z:" : ":NZ:";
                str += this.ConditionalParameter.ToString();
            }

            return str + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
        }

        public Instruction(byte code, IList<Parameter> parameters, Parameter conditionalParameter, bool conditionalZero) {
            this.Code = code;
            this.Length = (byte)(1 + (this.ConditionalParameter?.Length ?? 0));
            this.ConditionalParameter = conditionalParameter;
            this.ConditionalZero = conditionalZero;
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

        public Instruction(IWordStream stream, ulong address) {
            var instruction = stream.ReadWord(address++);

            this.Code = (byte)((instruction & 0xFF00000000000000UL) >> 56);
            this.Length = 1;
            this.Definition = InstructionDefinition.Find(this.Code);

            if ((instruction & 0x200000) != 0) {
                this.ConditionalParameter = this.DecodeParameter((ParameterType)((instruction >> 17) & 0x03), (instruction & 0x80000) != 0, instruction << 31, 0, stream, ref address);
                this.ConditionalZero = (instruction & 0x100000) != 0;
            }

            if (this.Definition.ParameterCount >= 1) {
                this.Parameter1 = this.DecodeParameter((ParameterType)((instruction >> 6) & 0x03), (instruction & 0x100) != 0, instruction, 0, stream, ref address);
                this.Length += this.Parameter1.Length;
            }

            if (this.Definition.ParameterCount >= 2) {
                this.Parameter2 = this.DecodeParameter((ParameterType)((instruction >> 3) & 0x03), (instruction & 0x20) != 0, instruction, 1, stream, ref address);
                this.Length += this.Parameter2.Length;
            }

            if (this.Definition.ParameterCount >= 3) {
                this.Parameter3 = this.DecodeParameter((ParameterType)((instruction >> 0) & 0x03), (instruction & 0x04) != 0, instruction, 2, stream, ref address);
                this.Length += this.Parameter3.Length;
            }
        }

        public void Encode(BinaryWriter writer) {
            var value = (ulong)this.Code << 56;

            if (this.ConditionalParameter != null) {
                value |= 0x200000;
                value |= this.ConditionalZero ? 0x100000UL : 0;
                value |= (ulong)this.ConditionalParameter.Type << 17;
                value |= this.ConditionalParameter.IsIndirect ? 0x80000UL : 0;
                value |= (this.ConditionalParameter.Type == ParameterType.Register ? (ulong)this.ConditionalParameter.Register : 0) << 9;
            }

            if (this.Parameter1 != null) {
                value |= (ulong)this.Parameter1.Type << 6;
                value |= this.Parameter1.IsIndirect ? 0x100UL : 0;
                value |= (this.Parameter1.Type == ParameterType.Register ? (ulong)this.Parameter1.Register : 0) << 40;
            }

            if (this.Parameter2 != null) {
                value |= (ulong)this.Parameter2.Type << 3;
                value |= this.Parameter2.IsIndirect ? 0x20UL : 0;
                value |= (this.Parameter2.Type == ParameterType.Register ? (ulong)this.Parameter2.Register : 0) << 32;
            }

            if (this.Parameter3 != null) {
                value |= (ulong)this.Parameter3.Type << 0;
                value |= this.Parameter3.IsIndirect ? 0x04UL : 0;
                value |= (this.Parameter3.Type == ParameterType.Register ? (ulong)this.Parameter3.Register : 0) << 24;
            }

            writer.Write(value);

            this.EncodeParameter(writer, this.ConditionalParameter);
            this.EncodeParameter(writer, this.Parameter1);
            this.EncodeParameter(writer, this.Parameter2);
            this.EncodeParameter(writer, this.Parameter3);
        }

        private void EncodeParameter(BinaryWriter writer, Parameter parameter) {
            if (parameter == null)
                return;

            switch (parameter.Type) {
                case ParameterType.Address:
                    writer.Write(parameter.Address);

                    break;

                case ParameterType.Calculated:
                    ulong format = 0;

                    this.EncodeCalculatedFormat(parameter.Base, 0, ref format);
                    this.EncodeCalculatedFormat(parameter.Index, 1, ref format);
                    this.EncodeCalculatedFormat(parameter.Scale, 2, ref format);
                    this.EncodeCalculatedFormat(parameter.Offset, 3, ref format);

                    writer.Write((ulong)format);

                    this.EncodeParameter(writer, parameter.Base.Parameter);
                    this.EncodeParameter(writer, parameter.Index?.Parameter);
                    this.EncodeParameter(writer, parameter.Scale?.Parameter);
                    this.EncodeParameter(writer, parameter.Offset?.Parameter);

                    break;
            }
        }

        private void EncodeCalculatedFormat(Parameter.Calculated operand, int parameter, ref ulong format) {
            if (operand == null)
                return;

            format |= (operand.Parameter.Type == ParameterType.Register ? (ulong)operand.Parameter.Register : 0) << (40 - 8 * parameter);
            format |= (((ulong)operand.Parameter.Type << 1) | (operand.Parameter.IsIndirect ? 0x08UL : 0) | (operand.IsPositive ? 1UL : 0UL)) << (60 - 4 * parameter);
        }

        private Parameter DecodeParameter(ParameterType type, bool isIndirect, ulong instruction, int parameter, IWordStream stream, ref ulong address) {
            switch (type) {
                default: return null;
                case ParameterType.Register: return Parameter.CreateRegister(isIndirect, (Register)((instruction >> (40 - 8 * parameter)) & 0xFF));
                case ParameterType.Address: return Parameter.CreateAddress(isIndirect, stream.ReadWord(address++));
                case ParameterType.Stack: return Parameter.CreateStack(isIndirect);
                case ParameterType.Calculated:
                    var format = stream.ReadWord(address++);

                    var @base = this.DecodeCalculatedParameter(format, 0, stream, ref address);
                    var index = this.DecodeCalculatedParameter(format, 1, stream, ref address);
                    var scale = this.DecodeCalculatedParameter(format, 2, stream, ref address);
                    var offset = this.DecodeCalculatedParameter(format, 3, stream, ref address);

                    return Parameter.CreateCalculated(isIndirect, @base, index, scale, offset);
            }
        }

        private Parameter.Calculated DecodeCalculatedParameter(ulong format, int parameter, IWordStream stream, ref ulong address) {
            var adjusted = format >> (60 - 4 * parameter);

            var type = (ParameterType)((adjusted & 0x06) >> 1);
            var isIndirect = (adjusted & 0x08) != 0;
            var isPositive = (adjusted & 0x01) != 0;

            if (type == ParameterType.Calculated)
                return null;

            return new Parameter.Calculated(this.DecodeParameter(type, isIndirect, format, parameter, stream, ref address), isPositive);
        }
    }
}