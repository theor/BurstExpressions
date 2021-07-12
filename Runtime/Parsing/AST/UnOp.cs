namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct UnOp : IOp
    {
        public readonly OpType Type;
        public readonly INode A;

        public UnOp(OpType type, INode a)
        {
            Type = type;
            A = a;
        }

        public override string ToString() => $"{Parser.Ops[Type].Str}{A}";
    }
}