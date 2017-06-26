﻿namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public sealed class XchgStatementNode : IntrinsicStatementNode {
        public ArgumentListNode ArgumentList { get; }

        public XchgStatementNode(ArgumentListNode argumentList) => this.ArgumentList = argumentList;
    }
}