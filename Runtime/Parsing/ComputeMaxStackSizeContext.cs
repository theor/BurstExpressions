using BurstExpressions.Runtime.Runtime;
using Unity.Mathematics;
using UnityEngine;

namespace BurstExpressions.Runtime.Parsing
{
    public struct ComputeMaxStackSizeContext : IContext
    {
        private int _stackLength;
        public int MaxStackLength { get; private set; }

        private int StackLength
        {
            get => _stackLength;
            set
            {
                MaxStackLength = Mathf.Max(value, MaxStackLength);
                _stackLength = value;
            }
        }
        public float3 Param(byte paramIndex) => default;

        public float3 Load(byte paramIndex) => default;

        public float3 Pop()
        {
            StackLength--;
            return default;
        }

        public void Push(float3 val)
        {
            StackLength++;
        }

        public static int ComputeMaxStackSize(EvaluationInstruction[] evaluationInstructions)
        {
            var current = 0;
            var defaultOps = default(Evaluator.DefaultOps);
            ComputeMaxStackSizeContext ctx = default;
            var count = evaluationInstructions.Length;
            while (current < count)
            {
                var node = evaluationInstructions[current];
                defaultOps.ExecuteOp(node, ref ctx);
                current++;
            }

            return ctx.MaxStackLength;
        }
    }
}