using System;
using System.Collections.Generic;

namespace ArkeOS.Tools.KohlCompiler.IR {
    public sealed class BasicBlockCreator {
        private readonly List<BasicBlock> allBlocks = new List<BasicBlock>();
        private BasicBlock currentBlock;

        private BasicBlock NewBlock() {
            var blk = new BasicBlock();

            this.allBlocks.Add(blk);

            return blk;
        }

        public IReadOnlyCollection<BasicBlock> AllBlocks => this.allBlocks;
        public BasicBlock Entry { get; }

        public BasicBlockCreator() => this.currentBlock = this.Entry = this.NewBlock();

        private BasicBlock SetBlock(BasicBlock block) => this.currentBlock = block;
        private T SetTerminator<T>(T terminator) where T : Terminator => this.currentBlock.Terminator == null ? (T)(this.currentBlock.Terminator = terminator) : throw new InvalidOperationException();

        public BasicBlock PushNew() => this.SetBlock(this.NewBlock());
        public BasicBlock PushNew(BasicBlock block) => this.SetBlock(block);

        public (T, BasicBlock) PushTerminator<T>(T terminator) where T : Terminator => (this.SetTerminator(terminator), this.PushNew());
        public (T, BasicBlock) PushTerminator<T>(T terminator, BasicBlock next) where T : Terminator => (this.SetTerminator(terminator), this.PushNew(next));

        public void PushInstuction(BasicBlockInstruction bbi) => this.currentBlock.Instructions.Add(bbi);
    }
}
