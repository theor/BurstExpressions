using System.Collections.Generic;

namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct FuncCall : IAstOp
    {
        public readonly string Id;
        public readonly List<IAstNode> Arguments;

        public FuncCall(string id, List<IAstNode> arguments)
        {
            Id = id;
            Arguments = arguments;
        }

        public override string ToString() => $"#{Id}({string.Join(", ", Arguments)})";
    }
}