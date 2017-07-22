using ArkeOS.Utilities;
using ArkeOS.Utilities.Extensions;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Hardware.Architecture {
    public class Instruction {
        public byte Code { get; }
        public byte Length { get; private set; }
        public InstructionDefinition Definition { get; }

        public Parameter ConditionalParameter { get; }
        public InstructionConditionalType ConditionalType { get; }

        public Parameter Parameter1 { get; }
        public Parameter Parameter2 { get; }
        public Parameter Parameter3 { get; }

        public override string ToString() => this.ToString(16);

        public string ToString(int radix) {
            var str = string.Empty;

            if (this.ConditionalParameter != null) {
                str += this.ConditionalType == InstructionConditionalType.WhenZero ? "IFZ " : "IFNZ ";
                str += this.ConditionalParameter.ToString(radix) + " ";
            }

            return (str + this.Definition.Mnemonic + " " + this.Parameter1?.ToString(radix) + " " + this.Parameter2?.ToString(radix) + " " + this.Parameter3?.ToString(radix)).Trim();
        }

        public Instruction(byte code, IList<Parameter> parameters) : this(code, parameters, null, default(InstructionConditionalType)) { }
        public Instruction(byte code, IList<Parameter> parameters, Parameter conditionalParameter, InstructionConditionalType conditionalType) : this(InstructionDefinition.Find(code), parameters, conditionalParameter, conditionalType) { }

        public Instruction(InstructionDefinition def, IList<Parameter> parameters) : this(def, parameters, null, default(InstructionConditionalType)) { }

        public Instruction(InstructionDefinition def, IList<Parameter> parameters, Parameter conditionalParameter, InstructionConditionalType conditionalType) {
            this.Definition = def;
            this.Code = def.Code;
            this.Length = (byte)(1 + (conditionalParameter?.Length ?? 0));
            this.ConditionalParameter = conditionalParameter;
            this.ConditionalType = conditionalType;

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
            var bits = new BitStream(stream.ReadWord(address++));

            this.Length = 1;
            this.Code = bits.ReadU8(8);
            this.Definition = InstructionDefinition.Find(this.Code);

            if (this.Definition.ParameterCount > 0) this.Parameter1 = this.DecodeParameter(stream, ref address, bits);
            if (this.Definition.ParameterCount > 1) this.Parameter2 = this.DecodeParameter(stream, ref address, bits);
            if (this.Definition.ParameterCount > 2) this.Parameter3 = this.DecodeParameter(stream, ref address, bits);

            bits.Advance((3 - this.Definition.ParameterCount) * 8);

            if (bits.ReadU1()) {
                this.ConditionalType = bits.ReadU1() ? InstructionConditionalType.WhenNotZero : InstructionConditionalType.WhenZero;
                this.ConditionalParameter = this.DecodeParameter(stream, ref address, bits);
            }
        }

        private Parameter DecodeParameter(IWordStream stream, ref ulong address, BitStream bits) {
            var para = new Parameter() {
                IsIndirect = bits.ReadU1(),
                RelativeTo = (ParameterRelativeTo)bits.ReadU8(2),
                Type = (ParameterType)bits.ReadU8(2),
                Register = (Register)bits.ReadU8(5)
            };

            if (para.Type == ParameterType.Literal) {
                para.Literal = stream.ReadWord(address++);

                this.Length++;
            }

            return para;
        }

        public void Encode(BinaryWriter writer) {
            var origPosition = writer.BaseStream.Position;
            var bits = new BitStream();

            writer.Write(0UL);
            bits.Write(this.Code, 8);

            if (this.Definition.ParameterCount > 0) this.EncodeParameter(writer, bits, this.Parameter1);
            if (this.Definition.ParameterCount > 1) this.EncodeParameter(writer, bits, this.Parameter2);
            if (this.Definition.ParameterCount > 2) this.EncodeParameter(writer, bits, this.Parameter3);

            bits.Advance((3 - this.Definition.ParameterCount) * 8);

            if (this.ConditionalParameter != null) {
                bits.Write(true);
                bits.Write(this.ConditionalType == InstructionConditionalType.WhenNotZero);

                this.EncodeParameter(writer, bits, this.ConditionalParameter);
            }

            writer.WriteAt(bits.Word, origPosition);
        }

        private void EncodeParameter(BinaryWriter writer, BitStream bits, Parameter parameter) {
            bits.Write(parameter.IsIndirect);
            bits.Write((byte)parameter.RelativeTo, 2);
            bits.Write((byte)parameter.Type, 2);
            bits.Write((byte)parameter.Register, 5);

            if (parameter.Type == ParameterType.Literal)
                writer.Write(parameter.Literal);
        }
    }
}
