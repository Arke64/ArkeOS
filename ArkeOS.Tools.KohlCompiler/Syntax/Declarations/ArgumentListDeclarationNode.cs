namespace ArkeOS.Tools.KohlCompiler.Syntax {
    public class ArgumentListDeclarationNode : SyntaxListNode<ArgumentDeclarationNode> {
        public bool Extract(int index, out ArgumentDeclarationNode arg) {
            if (index < this.Items.Count) {
                arg = this.Items[index];
                return true;
            }
            else {
                arg = default(ArgumentDeclarationNode);
                return false;
            }
        }

        public bool Extract(out ArgumentDeclarationNode arg0) => this.Extract(0, out arg0);
        public bool Extract(out ArgumentDeclarationNode arg0, out ArgumentDeclarationNode arg1) { arg0 = arg1 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1); }
        public bool Extract(out ArgumentDeclarationNode arg0, out ArgumentDeclarationNode arg1, out ArgumentDeclarationNode arg2) { arg0 = arg1 = arg2 = null; return this.Extract(0, out arg0) && this.Extract(1, out arg1) && this.Extract(2, out arg2); }
    }
}
