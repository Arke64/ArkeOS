using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkeOS.ISA;

namespace ArkeOS.Executable {
	public class DataSection : Section {
		private List<Parameter> parameters;

		public DataSection(Image parent, ulong address) : base(parent, address, false) {
			this.parameters = new List<Parameter>();
		}

		public DataSection(ulong address, ulong size, byte[] data) : base(address, size, false, data) {

		}

		public void AddLiteral(string[] parts, string pendingLabel) {
			if (pendingLabel != string.Empty)
				this.Parent.Labels[pendingLabel] = this.CurrentAddress;

			var size = InstructionSize.EightByte;

			switch (parts[0]) {
				case "U1": size = InstructionSize.OneByte; break;
				case "U2": size = InstructionSize.TwoByte; break;
				case "U4": size = InstructionSize.FourByte; break;
			}

			this.CurrentAddress += Instruction.SizeToBytes(size);

			this.parameters.Add(new Parameter(size, parts[1]));
		}

		public override void Serialize(BinaryWriter writer) {
			foreach (var p in this.parameters) {
				if (!string.IsNullOrWhiteSpace(p.Label)) {
					p.Literal = this.Parent.Labels[p.Label];
					p.Type = ParameterType.Literal;
				}
			}

			this.Size = (ulong)this.parameters.Sum(i => i.Length);

			base.Serialize(writer);

			this.parameters.ForEach(i => i.Serialize(writer));
		}
	}
}