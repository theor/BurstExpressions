namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct Variable : IVal
    {
        public readonly string Id;

        public Variable(string id)
        {
            Id = id;
        }

        public override string ToString() => $"${Id}";
    }
}