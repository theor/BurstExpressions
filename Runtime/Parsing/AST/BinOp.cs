namespace Eval
{
    public struct BinOp : IOp
    {
        public readonly OpType Type;
        public readonly INode A;
        public readonly INode B;

        public BinOp(OpType type, INode a, INode b)
        {
            Type = type;
            A = a;
            B = b;
        }

        public override string ToString() => $"({A} {Parser.Ops[Type].Str} {B})";
    }
}