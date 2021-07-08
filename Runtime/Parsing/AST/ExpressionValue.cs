using System.Globalization;

namespace Eval
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