using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Eval.Runtime
{
    [BurstCompile]
    public struct EvalJob : IJob
    {
        public EvalGraph EvalGraph;
        public NativeReference<float3> Result;
        [NativeDisableUnsafePtrRestriction]
        public unsafe float3* Params;
        public unsafe void Execute()
        {
            EvalState state = new EvalState();
            Result.Value = state.Run(EvalGraph, Params);
        }
    }
}