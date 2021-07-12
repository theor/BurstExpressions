using System.Globalization;

namespace BurstExpressions.Runtime.Parsing.AST
{
    public struct ExpressionValue : IVal
    {
        public readonly float F;

        public ExpressionValue(float f)
        {
            F = f;
        }

        public override string ToString() => F.ToString(CultureInfo.InvariantCulture);
    }
}