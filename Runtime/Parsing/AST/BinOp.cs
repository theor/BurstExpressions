namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct BinOp : IAstOp
    {
        public readonly OpType Type;
        public readonly IAstNode A;
        public readonly IAstNode B;

        public BinOp(OpType type, IAstNode a, IAstNode b)
        {
            Type = type;
            A = a;
            B = b;
        }

        public override string ToString() => $"({A} {Parser.Ops[Type].Str} {B})";
    }
}