using System.Collections.Generic;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class ConfigurationManager : BusDevice {
        private ulong[] interruptVectors;

        public byte ProtectionMode { get; private set; }
        public byte SystemTickInterval { get; private set; }
        public bool InstructionCachingEnabled { get; private set; }

        public IReadOnlyList<ulong> InterruptVectors => this.interruptVectors;

        public override ulong VendorId => 1;
        public override ulong ProductId => 1;

        public ConfigurationManager() {
            this.Reset();
        }

        public override ulong ReadWord(ulong address) {
            if (address == 0) {
                return this.ProtectionMode;
            }
            else if (address == 1) {
                return this.SystemTickInterval;
            }
            else if (address == 2) {
                return this.InstructionCachingEnabled ? 1UL : 0UL;
            }
            else if (address >= 0x10000 && address < 0x11010) {
                return this.interruptVectors[(int)address - 0x10000];
            }
            else {
                return 0;
            }
        }

        public override void WriteWord(ulong address, ulong data) {
            if (address == 0) {
                this.ProtectionMode = (byte)data;
            }
            else if (address == 0) {
                this.SystemTickInterval = (byte)data;
            }
            else if (address == 0) {
                this.InstructionCachingEnabled = data != 0;
            }
            else if (address >= 0x10000 && address < 0x11010) {
                this.interruptVectors[(int)address - 0x10000] = data;
            }
        }

        public void Reset() {
            this.SystemTickInterval = 50;
            this.InstructionCachingEnabled = true;
            this.ProtectionMode = 0;

            this.interruptVectors = new ulong[0x11010];
        }
    }
}