using BurstExpressions.Runtime.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public class EvaluationTests : EvaluationTestsBase
{
    [BurstCompile]
    public struct EvaluationJob : IJobParallelFor
    {
        public EvaluationGraph EvaluationGraph;
        public NativeArray<float3> Results;
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> Params;

        public unsafe void Execute(int index)
        {
            Evaluator state = new Evaluator();
            NativeSlice<float3> nativeSlice = Params.Slice(index * EvaluationGraph.ParameterCount, EvaluationGraph.ParameterCount);
            Results[index] = state.Run(EvaluationGraph, new Evaluator.DefaultOps(), (float3*)nativeSlice.GetUnsafeReadOnlyPtr(), nativeSlice.Length);
        }
    }

    [Test]
    public void ConstFloat3() =>
        Run(new float3(1, 2, 3), new[] { new EvaluationInstruction(EvalOp.Const_0, new float3(1, 2, 3)) }, 1, 10);

    [Test]
    public void Test_LD() =>
        Run(new float3(.5f), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(5)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(10)),
            EvaluationInstruction.Ld(2),
            EvaluationInstruction.Ld(1),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 3, 10);

    [Test]
    public void Test_LD2() =>
        Run(new float3(.5f), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(10)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(5)),
            // 5 / 10
            EvaluationInstruction.Ld(1),
            EvaluationInstruction.Ld(2),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 3, 10);

    [Test]
    public void Params() =>
        Run(new float3(1, 2, 3), new[]
        {
            EvaluationInstruction.Param(1),
            EvaluationInstruction.Param(2),
            new EvaluationInstruction(EvalOp.Add_2),
        }, 1, 10, new float3(1, 2, 0), new float3(0, 0, 3));

    [Test]
    public void AddFloat3() =>
        Run(new float3(5, 7, 9), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(1, 2, 3)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(4, 5, 6)),
            new EvaluationInstruction(EvalOp.Add_2),
        }, 1, 10);

    [Test]
    public void Div() =>
        Run(new float3(2), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, 3f),
            new EvaluationInstruction(EvalOp.Const_0, 6f),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 1, 10);
}