namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct UnOp : IAstOp
    {
        public readonly OpType Type;
        public readonly IAstNode A;

        public UnOp(OpType type, IAstNode a)
        {
            Type = type;
            A = a;
        }

        public override string ToString() => $"{Parser.Ops[Type].Str}{A}";
    }
}