﻿namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class LocalVariableDeclarationNode : DeclarationNode {
        public LocalVariableDeclarationNode(Token identifier) : base(identifier) { }
        public LocalVariableDeclarationNode(string identifier) : base(identifier) { }
    }
}