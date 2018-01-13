using ArkeOS.Utilities;
using ArkeOS.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Hardware.Architecture {
    public class Instruction {
        public byte Code { get; }
        public InstructionDefinition Definition { get; }

        public Parameter ConditionalParameter { get; }
        public InstructionConditionalType ConditionalType { get; }

        public Parameter Parameter1 { get; }
        public Parameter Parameter2 { get; }
        public Parameter Parameter3 { get; }

        public byte Length => (byte)(1 + (this.Parameter1?.Length ?? 0) + (this.Parameter2?.Length ?? 0) + (this.Parameter3?.Length ?? 0) + (this.ConditionalParameter?.Length ?? 0));

        public override string ToString() => this.ToString(16);

        public string ToString(int radix) => this.ToString(radix, ulong.MaxValue, null, null);

        public string ToString(int radix, ulong currentInstructionOffset, string instructionFormatString, IReadOnlyDictionary<ulong, string> rbpOffsetNames) {
            var str = string.Empty;

            if (this.ConditionalParameter != null) {
                str += this.ConditionalType == InstructionConditionalType.WhenZero ? "IFZ " : "IFNZ ";
                str += this.ConditionalParameter.ToString(radix, currentInstructionOffset, instructionFormatString, rbpOffsetNames) + " ";
            }

            return (str + this.Definition.Mnemonic + " " + this.Parameter1?.ToString(radix, currentInstructionOffset, instructionFormatString, rbpOffsetNames) + " " + this.Parameter2?.ToString(radix, currentInstructionOffset, instructionFormatString, rbpOffsetNames) + " " + this.Parameter3?.ToString(radix, currentInstructionOffset, instructionFormatString, rbpOffsetNames)).Trim();
        }

        public Instruction(byte code, IList<Parameter> parameters) : this(code, parameters, null, default(InstructionConditionalType)) { }
        public Instruction(byte code, IList<Parameter> parameters, Parameter conditionalParameter, InstructionConditionalType conditionalType) : this(InstructionDefinition.Find(code), parameters, conditionalParameter, conditionalType) { }

        public Instruction(InstructionDefinition def, IList<Parameter> parameters) : this(def, parameters, null, default(InstructionConditionalType)) { }

        public Instruction(InstructionDefinition def, IList<Parameter> parameters, Parameter conditionalParameter, InstructionConditionalType conditionalType) {
            this.Definition = def;
            this.Code = def.Code;
            this.ConditionalParameter = conditionalParameter;
            this.ConditionalType = conditionalType;

            if (this.Definition.ParameterCount >= 1)
                this.Parameter1 = parameters[0];

            if (this.Definition.ParameterCount >= 2)
                this.Parameter2 = parameters[1];

            if (this.Definition.ParameterCount >= 3)
                this.Parameter3 = parameters[2];
        }

        public Instruction(IWordStream stream, ulong address) {
            var bits = new BitStream(stream.ReadWord(address++));

            this.Code = bits.ReadU8(8);
            this.Definition = InstructionDefinition.Find(this.Code);

            if (this.Definition.ParameterCount > 0) this.Parameter1 = this.DecodeParameter(stream, ref address, bits);
            if (this.Definition.ParameterCount > 1) this.Parameter2 = this.DecodeParameter(stream, ref address, bits);
            if (this.Definition.ParameterCount > 2) this.Parameter3 = this.DecodeParameter(stream, ref address, bits);

            bits.Advance((3 - this.Definition.ParameterCount) * 13);

            if (bits.ReadU1()) {
                this.ConditionalType = bits.ReadU1() ? InstructionConditionalType.WhenNotZero : InstructionConditionalType.WhenZero;
                this.ConditionalParameter = this.DecodeParameter(stream, ref address, bits);
            }
        }

        private Parameter DecodeParameter(IWordStream stream, ref ulong address, BitStream bits) {
            var indirect = bits.ReadU1();
            var relative = (ParameterRelativeTo)bits.ReadU8(2);
            var type = (ParameterType)bits.ReadU8(2);
            var literal = bits.ReadU64(8);
            var register = (Register)literal;
            var forbid = false;

            if (type == ParameterType.Literal) {
                literal = stream.ReadWord(address++);
                forbid = true;
            }
            else if (type == ParameterType.EmbeddedLiteral) {
                type = ParameterType.Literal;
            }

            return new Parameter() {
                IsIndirect = indirect,
                RelativeTo = relative,
                Type = type,
                Register = register,
                Literal = literal,
                ForbidEmbedded = forbid,
            };
        }

        public void Encode(BinaryWriter writer) {
            var origPosition = writer.BaseStream.Position;
            var bits = new BitStream();

            writer.Write(0UL);
            bits.Write(this.Code, 8);

            if (this.Definition.ParameterCount > 0) this.EncodeParameter(writer, bits, this.Parameter1);
            if (this.Definition.ParameterCount > 1) this.EncodeParameter(writer, bits, this.Parameter2);
            if (this.Definition.ParameterCount > 2) this.EncodeParameter(writer, bits, this.Parameter3);

            bits.Advance((3 - this.Definition.ParameterCount) * 13);

            if (this.ConditionalParameter != null) {
                bits.Write(true);
                bits.Write(this.ConditionalType == InstructionConditionalType.WhenNotZero);

                this.EncodeParameter(writer, bits, this.ConditionalParameter);
            }

            writer.WriteAt(bits.Word, origPosition);
        }

        private void EncodeParameter(BinaryWriter writer, BitStream bits, Parameter parameter) {
            var type = parameter.Type == ParameterType.Literal && (parameter.Literal <= Parameter.MaxForEmbeddedLiteral && !parameter.ForbidEmbedded) ? ParameterType.EmbeddedLiteral : parameter.Type;
            var literal = parameter.Type == ParameterType.Register ? (byte)parameter.Register : parameter.Literal;

            bits.Write(parameter.IsIndirect);
            bits.Write((byte)parameter.RelativeTo, 2);
            bits.Write((byte)type, 2);
            bits.Write(literal, 8);

            if (type == ParameterType.Literal)
                writer.Write(parameter.Literal);
        }
    }
}
