using System.Collections.Generic;
using Unity.Mathematics;

namespace BurstExpressions.Runtime.Parsing
{
    public static class Constants
    {
        private static Dictionary<string, (float3, string)> s_Constants = new Dictionary<string, (float3, string)>
        {
            { "PI", (math.PI, "The mathematical constant pi. Approximately 3.14.") },
            { "E", (math.E, "The mathematical constant e also known as Euler's number. Approximately 2.72.") },
            { "SQRT2", (math.SQRT2, "The square root 2. Approximately 1.41.") },
        };

        public static bool TryGetValue(string name, out float3 value)
        {
            if (s_Constants.TryGetValue(name, out var v))
            {
                value = v.Item1;
                return true;
            }

            value = default;
            return false;
        }

        public static IReadOnlyDictionary<string, (float3, string)> AllConstants() => s_Constants;
    }
}