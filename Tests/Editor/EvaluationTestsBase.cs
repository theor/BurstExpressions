using System.Collections.Generic;
using System.Linq;
using Eval;
using Eval.Runtime;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class EvaluationTestsBase
{
    protected unsafe void Run(float3 result, IEnumerable<EvalGraph.Node> nodes, byte expectedFinalStackLength, byte maxStackSize, params float3[] @params)
    {
        EvalJob j = default;
        try
        {
            fixed (float3* paramsPtr = @params)
            {
                j = new EvalJob
                {
                    EvalGraph = new EvalGraph(nodes.ToArray(), expectedFinalStackLength, maxStackSize),
                    Result = new NativeReference<float3>(Allocator.TempJob),
                    Params = paramsPtr,
                };
                j.Run();
            }

            Debug.Log($"Result: {j.Result.Value}");
            Assert.AreEqual(result, j.Result.Value);
        }
        finally
        {
            j.EvalGraph.Dispose();
            j.Result.Dispose();
        }
    }

    protected void ParseRun(float3 result, bool simplify, string input, Dictionary<string, float3> variables, params (string, float3)[] @params)
    {
        var n = Parser.Parse(input, out var err);
        Assert.IsNull(err, err);
        var nodes = Translator.Translate(n,
            variables.Select(x => new FormulaParam(x.Key) { Value = x.Value }).ToList(),
            @params.Select(x => x.Item1).ToList(), out var usedValues,
            simplify ? Translator.TranslationOptions.FoldConstantExpressions : Translator.TranslationOptions.None);

        Debug.Log(string.Join("\n", variables.Select(x => $"{x.Key}: {x.Value}")));
        Debug.Log("Opcodes");
        Debug.Log(string.Join("\n", nodes));
        Run(result, nodes, 1, 10, @params.Select(x => x.Item2).ToArray());
    }
}