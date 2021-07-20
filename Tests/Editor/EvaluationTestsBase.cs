using System;
using System.Collections.Generic;
using System.Linq;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Runtime;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class EvaluationTestsBase
{
    protected unsafe void Run(float3 result, IEnumerable<EvaluationInstruction> nodes, byte expectedFinalStackLength, byte maxStackSize, params float3[] @params)
    {
        EvaluationTests.EvaluationJob j = default;
        try
        {
            var resultsNativeArray = new NativeArray<float3>(1, Allocator.TempJob);
            var paramsNativeArray = new NativeArray<float3>(@params ?? new float3[0], Allocator.TempJob);
            var evaluationGraph = new EvaluationGraph(nodes.ToArray(), expectedFinalStackLength, maxStackSize,
                (byte)(@params?.Length ?? 0));
            j = new EvaluationTests.EvaluationJob
            {
                EvaluationGraph = evaluationGraph,
                Results = resultsNativeArray,
                Params = paramsNativeArray,
            };
            j.Run(1);

            Debug.Log($"Results: {j.Results[0]}");
            Assert.AreEqual(result, j.Results[0]);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
        finally
        {
            j.EvaluationGraph.Dispose();
            j.Results.Dispose();
            j.Params.Dispose();
        }
    }

    protected void ParseRun(float3 result, bool simplify, string input, Dictionary<string, float3> variables, params (string, float3)[] @params)
    {
        if (!Parser.TryParse(input, out var n, out var err))
            Assert.IsTrue(false, err.ToString());
        var nodes = Translator.Translate(n,
            variables.Select(x => new NamedValue(x.Key) { Value = x.Value }).ToList(),
            @params.Select(x => x.Item1).ToList(), out var usedValues,
            simplify ? Translator.TranslationOptions.FoldConstantExpressions : Translator.TranslationOptions.None);

        Debug.Log(string.Join("\n", variables.Select(x => $"{x.Key}: {x.Value}")));
        Debug.Log("Opcodes");
        Debug.Log(string.Join("\n", nodes));
        Run(result, nodes, 1, 10, @params.Select(x => x.Item2).ToArray());
    }
}