using System.Collections.Generic;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEditor.Graphs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class PerformanceTests : EvaluationTestsBase
{
    protected interface IPerfCase
    {
        float3 Code(float input);
        string Expression { get; }
    }

    private struct ConstantCase : IPerfCase
    {
        public float3 Code(float input) => new float3(42);

        public string Expression => "42";
    }
    private struct V3SinMulCosMulCase : IPerfCase
    {
        public float3 Code(float input) => new float3(math.cos(input * 16), 0, math.sin(input * 12));

        public string Expression => "v3(cos(t*16), 0, sin(t*12))";
    }

    protected void PerfTest<T>(int elementCount) where T : struct, IPerfCase
    {
        var perfCase = default(T);
        const int measurementCount = 200;
        const int iterationsPerMeasurement = 10;
        const int warmupCount = 10;

        float[] inputs = new float[elementCount];
        float3[] outputs = new float3[elementCount];

        Random rnd = new Random(42);
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = rnd.NextFloat();
        }

        Measure.Method(() =>
            {
                for (int index = 0; index < elementCount; index++)
                {
                    var inputf = inputs[index];
                    float3 res = perfCase.Code(inputf);
                    outputs[index] = res;
                }
            })
            .SampleGroup("Code")
            .WarmupCount(warmupCount)
            .MeasurementCount(measurementCount)
            .IterationsPerMeasurement(iterationsPerMeasurement)
            .Run();

        string input = perfCase.Expression;
        if (!Parser.TryParse(input, out var n, out var err))
            Assert.IsTrue(false, err.ToString());
        EvaluationInstruction[] nodes = Translator.Translate(n,
            new List<NamedValue>
            {
                new NamedValue("x"){Value = new Vector3(42f, 0, 0)}
            },
            new List<string> { "t" }, out var usedValues);
        EvaluationGraph graph = new EvaluationGraph(nodes, 1, 10, 1, Allocator.Temp);


        var inputsNativeArray = new NativeArray<float>(inputs, Allocator.TempJob);
        var outputsNativeArray = new NativeArray<float3>(inputs.Length, Allocator.TempJob,
            NativeArrayOptions.UninitializedMemory);

        try
        {
            Measure.Method(() =>
                {
                    for (int index = 0; index < elementCount; index++)
                    {
                        var @params = (float3)inputs[index];
                        Evaluator.Run5(graph, @params, out float3 res);
                        outputs[index] = res;
                    }
                })
                .SampleGroup("Eval")
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();

            var job = new TestEvalJob
            {
                Graph = graph,
                Inputs = inputsNativeArray,
                Outputs = outputsNativeArray
            };
            Measure.Method(() =>
                {

                    job.Run(inputs.Length);

                })
                .SampleGroup("Eval Job, parallel")
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();

            var job2 = new TestEvalJob2
            {
                Graph = graph,
                Inputs = inputsNativeArray,
                Outputs = outputsNativeArray
            };
            Measure.Method(() =>
                {

                    job2.Run();

                })
                .SampleGroup("Eval Job, single threaded")
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();
            var jobCodeParallel = new TestCodeJob<T>()
            {
                Inputs = inputsNativeArray,
                Outputs = outputsNativeArray
            };
            Measure.Method(() =>
                {

                    jobCodeParallel.Run(inputs.Length);

                })
                .SampleGroup("Code Job, parallel")
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();
            var jobCode = new TestCodeJobSingle<T>()
            {
                Inputs = inputsNativeArray,
                Outputs = outputsNativeArray
            };
            Measure.Method(() =>
                {

                    jobCode.Run();

                })
                .SampleGroup("Code Job")
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .Run();
        }
        finally
        {
            inputsNativeArray.Dispose();
            outputsNativeArray.Dispose();
            graph.Dispose();
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TestEvalJob : IJobParallelFor
    {
        [ReadOnly]
        public EvaluationGraph Graph;
        [ReadOnly]
        public NativeArray<float> Inputs;
        [WriteOnly]
        public NativeArray<float3> Outputs;
        public void Execute(int index)
        {
            var @params = (float3)Inputs[index];
            Evaluator.Run5(Graph, @params, out float3 res);
            Outputs[index] = res;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TestCodeJob<T> : IJobParallelFor where T : struct, IPerfCase
    {
        [ReadOnly]
        public NativeArray<float> Inputs;
        [WriteOnly]
        public NativeArray<float3> Outputs;
        public void Execute(int index)
        {
            var @params = (float3)Inputs[index];

            Outputs[index] = default(T).Code(@params.x);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TestCodeJobSingle<T> : IJob where T : struct, IPerfCase
    {
        [ReadOnly, NoAlias]
        public NativeArray<float> Inputs;
        [WriteOnly, NoAlias]
        public NativeArray<float3> Outputs;
        public void Execute()
        {
            for (int index = 0; index < Inputs.Length; index++)
            {
                var @params = (float3)Inputs[index];
                Outputs[index] = default(T).Code(@params.x);
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TestEvalJob2 : IJob
    {
        [ReadOnly]
        public EvaluationGraph Graph;
        [ReadOnly]
        public NativeArray<float> Inputs;
        [WriteOnly]
        public NativeArray<float3> Outputs;
        public void Execute()
        {
            for (int index = 0; index < Inputs.Length; index++)
            {
                var @params = (float3)Inputs[index];
                Evaluator.Run5(Graph, @params, out float3 res);
                Outputs[index] = res;
            }
        }
    }

    [Test, Performance] public void TestPerfCaseV3SinMulCosMulCase([Values(1, 100, 1000, 10_000)] int count) => PerfTest<V3SinMulCosMulCase>(count);
    [Test, Performance] public void TestPerfCaseConstantCase([Values(1, 100, 1000, 10_000)] int count) => PerfTest<ConstantCase>(count);
}