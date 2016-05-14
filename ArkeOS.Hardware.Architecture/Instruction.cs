using System.Collections.Generic;
using System.IO;
using ArkeOS.Utilities;

namespace ArkeOS.Hardware.Architecture {
    public class Instruction {
        public byte Code { get; }
        public byte Length { get; private set; }
        public InstructionDefinition Definition { get; }

        public Parameter ConditionalParameter { get; }
        public bool ConditionalZero { get; }

        public Parameter Parameter1 { get; }
        public Parameter Parameter2 { get; }
        public Parameter Parameter3 { get; }

        public override string ToString() {
            var str = string.Empty;

            if (this.ConditionalParameter != null) {
                str += this.ConditionalZero ? "IFZ " : "IFNZ ";
                str += this.ConditionalParameter.ToString();
            }

            return str + " " + this.Definition.Mnemonic + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
        }

        public Instruction(byte code, IList<Parameter> parameters, Parameter conditionalParameter, bool conditionalZero) {
            this.Code = code;
            this.Length = (byte)(1 + (conditionalParameter?.Length ?? 0));
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
            var bits = new BitStream();

            bits.Reset(stream.ReadWord(address++));

            this.Length = 1;
            this.Code = bits.ReadU8(8);
            this.Definition = InstructionDefinition.Find(this.Code);

            this.Parameter1 = this.DecodeParameter(stream, ref address, bits, 1);
            this.Parameter2 = this.DecodeParameter(stream, ref address, bits, 2);
            this.Parameter3 = this.DecodeParameter(stream, ref address, bits, 3);

            if (bits.ReadU1()) {
                this.ConditionalZero = bits.ReadU1();

                this.ConditionalParameter = new Parameter();
                this.ConditionalParameter.IsRIPRelative = bits.ReadU1();
                this.ConditionalParameter.IsIndirect = bits.ReadU1();
                this.ConditionalParameter.Type = (ParameterType)bits.ReadU8(2);
                this.ConditionalParameter.Register = (Register)bits.ReadU8(5);

                if (this.ConditionalParameter.Type == ParameterType.Address) {
                    this.ConditionalParameter.Address = stream.ReadWord(address++);

                    this.Length++;
                }
            }
            else {
                bits.ReadU8(9);
            }
        }

        private Parameter DecodeParameter(IWordStream stream, ref ulong address, BitStream bits, int parameterNumber) {
            if (this.Definition.ParameterCount >= parameterNumber) {
                var para = new Parameter();

                para.IsRIPRelative = bits.ReadU1();
                para.IsIndirect = bits.ReadU1();
                para.Type = (ParameterType)bits.ReadU8(2);
                para.Register = (Register)bits.ReadU8(5);

                if (para.Type == ParameterType.Calculated) {
                    this.DecodeCalculatedParameter(stream, ref address, para);

                    this.Length++;
                }
                else if (para.Type == ParameterType.Address) {
                    para.Address = stream.ReadWord(address++);

                    this.Length++;
                }

                return para;
            }
            else {
                bits.ReadU8(8);

                return null;
            }
        }

        private void DecodeCalculatedParameter(IWordStream stream, ref ulong address, Parameter parameter) {
            var bits = new BitStream();

            bits.Reset(stream.ReadWord(address++));

            parameter.Base = this.DecodeCalculatedOperand(stream, ref address, bits);
            parameter.Index = this.DecodeCalculatedOperand(stream, ref address, bits);
            parameter.Scale = this.DecodeCalculatedOperand(stream, ref address, bits);
            parameter.Offset = this.DecodeCalculatedOperand(stream, ref address, bits);
        }

        private Parameter.Calculated DecodeCalculatedOperand(IWordStream stream, ref ulong address, BitStream bits) {
            var para = new Parameter();
            var sign = bits.ReadU1();

            para.IsRIPRelative = bits.ReadU1();
            para.IsIndirect = bits.ReadU1();
            para.Type = (ParameterType)bits.ReadU8(2);
            para.Register = (Register)bits.ReadU8(5);

            if (para.Type == ParameterType.Address) {
                para.Address = stream.ReadWord(address++);

                this.Length++;
            }

            if (para.Type == ParameterType.Calculated)
                return null;

            return new Parameter.Calculated(para, sign);
        }

        public void Encode(BinaryWriter writer) {
            var origPosition = writer.BaseStream.Position;
            writer.Write(0UL);

            var bits = new BitStream();
            bits.Write(this.Code, 8);

            this.EncodeParameter(writer, bits, this.Parameter1);
            this.EncodeParameter(writer, bits, this.Parameter2);
            this.EncodeParameter(writer, bits, this.Parameter3);

            if (this.ConditionalParameter != null) {
                bits.Write(true);
                bits.Write(this.ConditionalZero);
                bits.Write(this.ConditionalParameter.IsRIPRelative);
                bits.Write(this.ConditionalParameter.IsIndirect);
                bits.Write((byte)this.ConditionalParameter.Type, 2);
                bits.Write((byte)this.ConditionalParameter.Register, 5);
            }

            writer.BaseStream.Seek(origPosition, SeekOrigin.Begin);
            writer.Write(bits.Word);
            writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
        }

        private void EncodeParameter(BinaryWriter writer, BitStream bits, Parameter parameter) {
            if (parameter != null) {
                bits.Write(parameter.IsRIPRelative);
                bits.Write(parameter.IsIndirect);
                bits.Write((byte)parameter.Type, 2);
                bits.Write((byte)parameter.Register, 5);

                if (parameter.Type == ParameterType.Calculated) {
                    this.EncodeCalculatedParameter(writer, parameter);
                }
                else if (parameter.Type == ParameterType.Address) {
                    writer.Write(parameter.Address);
                }
            }
            else {
                bits.Write(0, 8);
            }
        }

        private void EncodeCalculatedParameter(BinaryWriter writer, Parameter parameter) {
            var bits = new BitStream();

            var origPosition = writer.BaseStream.Position;
            writer.Write(0UL);

            this.EncodeCalculatedOperand(writer, bits, parameter.Base);
            this.EncodeCalculatedOperand(writer, bits, parameter.Index);
            this.EncodeCalculatedOperand(writer, bits, parameter.Scale);
            this.EncodeCalculatedOperand(writer, bits, parameter.Offset);

            writer.BaseStream.Seek(origPosition, SeekOrigin.Begin);
            writer.Write(bits.Word);
            writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
        }

        private void EncodeCalculatedOperand(BinaryWriter writer, BitStream bits, Parameter.Calculated parameter) {
            if (parameter != null) {
                bits.Write(parameter.IsPositive);
                bits.Write(parameter.Parameter.IsRIPRelative);
                bits.Write(parameter.Parameter.IsIndirect);
                bits.Write((byte)parameter.Parameter.Type, 2);
                bits.Write((byte)parameter.Parameter.Register, 5);

                if (parameter.Parameter.Type == ParameterType.Address)
                    writer.Write(parameter.Parameter.Address);
            }
            else {
                bits.Write(0, 9);
            }
        }
    }
}