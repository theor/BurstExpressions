using System;
using System.Collections.Generic;
using System.Linq;
using Eval;
using Eval.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

public class ParsingEvaluationTests : EvaluationTestsBase
{
    static IEnumerable<TestCaseData> Cases
    {
        get
        {
            TestCaseData F(float3 a, string s, params (string, float3)[] @params) => new TestCaseData(a, s, @params).SetName($"{s} = {a} {string.Join(", ", @params)}");
            yield return F(1, "1");
            yield return F(3, "1+2");
            yield return F(3, "1+x", ("x", 2));
            yield return F(-1, "x(a) - 2", ("a", new float3(1, 2, 3)));
            yield return F(1, "z(a) - 2", ("a", new float3(1, 2, 3)));
        }
    }

    static IEnumerable<TestCaseData> SubCases
    {
        get
        {
            TestCaseData F(float3 a, string s, params (string, string)[] @params) => new TestCaseData(true, s, a, @params).SetName($"{s} = {a} {string.Join(", ", @params)}");
            TestCaseData Error(float3 a, string s, params (string, string)[] @params) => new TestCaseData(false, s, a, @params).SetName($"Error: {s} = {a} {string.Join(", ", @params)}");
            yield return F(10, "x+x", ("x", "5"));
            yield return F(10, "x+x", ("x", "2+3"));
            yield return F(5, "x", ("x", "y"), ("y", "5"));
            yield return F(10, "x", ("x", "y*2"), ("y", "5"));
            yield return F(30, "x*3", ("x", "y*2"), ("y", "5"));
            yield return F(10, "x+x", ("x", "y"), ("y", "5"));
            yield return F(8, "x+y", ("x", "5"), ("y", "3"));
            yield return F(8, "y+x", ("x", "5"), ("y", "3"));
            yield return F(32, "x", ("x", "y+z"), ("y", "3*z"), ("z", "8"));

            // recursive variable definition
            yield return Error(8, "y", ("y", "x"), ("x", "y"));
        }
    }

    [TestCaseSource(nameof(Cases))]
    public void ParseRunTest(float3 result, string input, (string, float3)[] @params) =>
        ParseRun(result, false, input, new Dictionary<string, float3>(), @params);
    [TestCaseSource(nameof(Cases))]
    public void ParseSimplifyRunTest(float3 result, string input, (string, float3)[] @params) =>
        ParseRun(result, true, input, new Dictionary<string, float3>(), @params);


    [TestCase("1 + 2")]
    [TestCase("1 * 2 + 3")]
    [TestCase("1 * 2 + 3 * 4")]
    [TestCase("x")]
    [TestCase("1 * 2 + x * 4")]
    [TestCase("pow(2,8)")]
    [TestCase("pow(2,8) * 2 + 1")]
    [TestCase("pow(2,8) * 2 + 1 + x")]
    [TestCase("x + pow(2,8) * 2 + 1")]
    [TestCase("x + (pow(2,8) * 2 + 1)")]
    [TestCase("p * 2 + p")]
    public void Simplify(string input)
    {
        var n = Parser.Parse(input, out _);
        var folded = Translator.Translate(n, new List<FormulaParam> { new FormulaParam("p") { Value = Vector3.one } }, new List<string> { "x" }, out _, Translator.TranslationOptions.FoldConstantExpressions);
        Debug.Log(Formatter.Format(n, Formatter.FormatFlags.DifferentColorPerNode | Formatter.FormatFlags.ParensAroundBinaryOperators));
        Debug.Log(String.Join("\n", folded));
    }


    [TestCaseSource(nameof(SubCases))]
    public void ParseRunSubFormulas(bool valid, string mainFormula, float3 result,
        params (string variable, string formula)[] formulas)
        => ParseRunSubFormulasSimplify(valid, false, mainFormula, result, formulas);
    [TestCaseSource(nameof(SubCases))]
    public void ParseRunSimplifiedSubFormulas(bool valid, string mainFormula, float3 result,
        params (string variable, string formula)[] formulas)
        => ParseRunSubFormulasSimplify(valid, true, mainFormula, result, formulas);
    public void ParseRunSubFormulasSimplify(bool valid, bool simplify, string mainFormula, float3 result, params (string variable, string formula)[] formulas)
    {
        var main = Parser.Parse(mainFormula, out var err);
        Assert.IsNull(err);


        var formulaParams = new List<FormulaParam>();
        foreach (var formula in formulas)
        {
            var x = Parser.Parse(formula.formula, out var xErr);
            Assert.IsNull(xErr);
            formulaParams.Add(FormulaParam.FromSubFormula(formula.variable, x));
        }
        formulaParams.Sort(Translator.FormulaParamsCompareByName);


        try
        {
            var nodes = Translator.Translate(main, formulaParams, new List<string> { "p" }, out var usedValues,
                simplify ? Translator.TranslationOptions.FoldConstantExpressions : Translator.TranslationOptions.None);
            Debug.Log(string.Join("\n", nodes));
            Run(result, nodes, (byte)(usedValues.NextIndex), 10, null);
        }
        catch (Exception)
        {
            if (valid)
                throw;
            return;
        }

        Assert.IsTrue(valid);
    }
    [Test]
    public void TestSubFormulas()
    {
        string input = "x+x";
        var main = Parser.Parse(input, out var err);
        var x = Parser.Parse("5", out var xErr);
        Assert.IsNull(err);
        Assert.IsNull(xErr);
        var nodes = Translator.Translate(main, new List<FormulaParam>
        {
            FormulaParam.FromSubFormula("x", x)
        }, null, out var usedValues);
        Run(10, nodes, (byte)(usedValues.NextIndex), 10, null);
    }
    [Test]
    public void PreserveParamsMultipleExecutions()
    {
        var variables = new Dictionary<string, float3>();
        ParseRun(1, false, "a", variables, ("a", 1), ("b", 2));
        ParseRun(2, false, "b", variables, ("a", 1), ("b", 2));
    }
}

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

public class EvaluationTests : EvaluationTestsBase
{
    // A Test behaves as an ordinary method
    [Test]
    public void ConstFloat3()
    {
        Run(new float3(1, 2, 3), new[] { new EvalGraph.Node(EvalOp.Const_0, new float3(1, 2, 3)) }, 1, 10);
    }
    [Test]
    public void Test_LD()
    {
        Run(new float3(.5f), new[]
        {
            new EvalGraph.Node(EvalOp.Const_0, new float3(5)),
            new EvalGraph.Node(EvalOp.Const_0, new float3(10)),
            EvalGraph.Node.Ld(2),
            EvalGraph.Node.Ld(1),
            new EvalGraph.Node(EvalOp.Div_2),
        }, 3, 10);
    }
    [Test]
    public void Test_LD2()
    {
        Run(new float3(.5f), new[]
        {
            new EvalGraph.Node(EvalOp.Const_0, new float3(10)),
            new EvalGraph.Node(EvalOp.Const_0, new float3(5)),
            // 5 / 10
            EvalGraph.Node.Ld(1),
            EvalGraph.Node.Ld(2),
            new EvalGraph.Node(EvalOp.Div_2),
        }, 3, 10);
    }

    [Test]
    public void Params()
    {
        Run(new float3(1, 2, 3), new[]
        {
            EvalGraph.Node.Param(1),
            EvalGraph.Node.Param(2),
            new EvalGraph.Node(EvalOp.Add_2),
        }, 1, 10, new float3(1, 2, 0), new float3(0, 0, 3));
    }

    [Test]
    public void AddFloat3()
    {
        Run(new float3(5, 7, 9), new[]
        {
            new EvalGraph.Node(EvalOp.Const_0, new float3(1, 2, 3)),
            new EvalGraph.Node(EvalOp.Const_0, new float3(4, 5, 6)),
            new EvalGraph.Node(EvalOp.Add_2),
        }, 1, 10);
    }

    [Test]
    public void Div()
    {
        Run(new float3(2), new[]
        {
            new EvalGraph.Node(EvalOp.Const_0, 3f),
            new EvalGraph.Node(EvalOp.Const_0, 6f),
            new EvalGraph.Node(EvalOp.Div_2),
        }, 1, 10);
    }
}