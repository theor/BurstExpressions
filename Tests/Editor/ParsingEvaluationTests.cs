using System;
using System.Collections.Generic;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Parsing;
using NUnit.Framework;
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
            yield return F(new float3(10, 20, 30), "x+x", ("x", "p*5"));
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
        Assert.IsTrue(Parser.TryParse(input, out var n, out var err), err.ToString());
        var folded = Translator.Translate(n, new List<NamedValue> { new NamedValue("p") { Value = Vector3.one } }, new List<string> { "x" }, out _, Translator.TranslationOptions.FoldConstantExpressions);
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
        Assert.IsTrue(Parser.TryParse(mainFormula, out var main, out var err), err.ToString());

        var formulaParams = new List<NamedValue>();
        foreach (var formula in formulas)
        {
            Assert.IsTrue(Parser.TryParse(formula.formula, out var x, out var xErr), err.ToString());
            formulaParams.Add(NamedValue.FromSubFormula(formula.variable, x));
        }
        formulaParams.Sort(Translator.FormulaParamsCompareByName);


        try
        {
            var nodes = Translator.Translate(main, formulaParams, new List<string> { "p" }, out var usedValues,
                simplify ? Translator.TranslationOptions.FoldConstantExpressions : Translator.TranslationOptions.None);
            Debug.Log(string.Join("\n", nodes));
            Run(result, nodes, (byte)(usedValues.NextIndex), 10, new float3[] { new float3(1, 2, 3) });
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
        Assert.IsTrue(Parser.TryParse(input, out var main, out var err), err.ToString());
        Assert.IsTrue(Parser.TryParse("5", out var x, out var xErr), err.ToString());
        var nodes = Translator.Translate(main, new List<NamedValue>
        {
            NamedValue.FromSubFormula("x", x)
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