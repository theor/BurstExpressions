using System.Collections.Generic;

namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct FuncCall : IOp
    {
        public readonly string Id;
        public readonly List<INode> Arguments;

        public FuncCall(string id, List<INode> arguments)
        {
            Id = id;
            Arguments = arguments;
        }

        public override string ToString() => $"#{Id}({string.Join(", ", Arguments)})";
    }
}