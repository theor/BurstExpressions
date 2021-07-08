using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eval;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Formatter = Eval.Formatter;

class ParsingTests
{
    [Test]
    public void Test()
    {
        Debug.Log(Formatter.Format(new BinOp(OpType.Add,
            new BinOp(OpType.Mul, new ExpressionValue(1), new ExpressionValue(2)), new ExpressionValue(3))));
    }

    public static IEnumerable<TestCaseData> Cases
    {
        get
        {
            yield return new TestCaseData("3+4", "(3 + 4)", 7f);
            yield return new TestCaseData("12*34", "(12 * 34)", 12f * 34f);
            yield return new TestCaseData("12*34+45", "((12 * 34) + 45)", 12 * 34 + 45f);
            yield return new TestCaseData("12+34*45", "(12 + (34 * 45))", 12 + 34 * 45f);
            yield return new TestCaseData("12+34+45", "((12 + 34) + 45)", 12 + 34 + 45f).SetDescription("Left associativity");
            yield return new TestCaseData("(32+4)", "(32 + 4)", 32 + 4f);
            yield return new TestCaseData("a", "$a", 7f);
            yield return new TestCaseData("1 * a+3", "((1 * $a) + 3)", 1 * 7 + 3f);
            // unary
            yield return new TestCaseData("-1", "-1", -1f);
            yield return new TestCaseData("--1", "--1", 1f);
            yield return new TestCaseData("-3+4", "(-3 + 4)", -3 + 4f);
            yield return new TestCaseData("3+-4", "(3 + -4)", 3 + -4f);
            yield return new TestCaseData("-(3+4)", "-(3 + 4)", -(3 + 4f));
            // coma
            // yield return new TestCaseData("1,2", "(1 , 2)");
            // yield return new TestCaseData("1,2,3", "(1 , (2 , 3))");
            // func calls
            yield return new TestCaseData("sqrt(64)", "sqrt(64)", 8f);
            yield return new TestCaseData("min(42, 43)", "min(42, 43)", 42f);
            yield return new TestCaseData("abs(-42)", "abs(-42)", 42f);
            yield return new TestCaseData("sqrt(63+1)", "sqrt((63 + 1))", 8f);
            yield return new TestCaseData("sqrt(abs(-64))", "sqrt(abs(-64))", 8f);
            yield return new TestCaseData("max(1, sqrt(4))", "max(1, sqrt(4))", 2f);
            yield return new TestCaseData("max(-1, abs(-4))", "max(-1, abs(-4))", 4f);
            yield return new TestCaseData("abs(abs(1+1/2))", "abs(abs((1 + (1 / 2))))", 1.5f);
            yield return new TestCaseData("tan(1)", "tan(1)", 1.55740774f);
            yield return new TestCaseData("tan(tan(1))", "tan(tan(1))", 74.6860046f);
            yield return new TestCaseData("tan(tan(11%10))", "tan(tan((11 % 10)))", 74.6860046f);
            yield return new TestCaseData("dist(a, 0.5) - 0.3 / 0.01*snoise(a + fbm(a)) ",
                "(dist($a, 0.5) - ((0.3 / 0.01) * snoise(($a + fbm($a)))))", null);
            yield return new TestCaseData("1*abs(a + 2) ", "(1 * abs(($a + 2)))", 9f);
            yield return new TestCaseData("v3(cos(t*f),sin(t*f),0)*1 + o", "((v3(cos(($t * $f)), sin(($t * $f)), 0) * 1) + $o)", null);
        }
    }

    [TestCaseSource("Cases")]
    public void Format(string input, string expectedFormat, float? _)
    {
        var node = Parser.Parse(input, out var error);
        void PrintFormat(Formatter.FormatFlags formatFlags) => Debug.Log(formatFlags + ":\n" + Formatter.Format(node, formatFlags));
        PrintFormat(Formatter.FormatFlags.None);
        PrintFormat(Formatter.FormatFlags.Colors);
        PrintFormat(Formatter.FormatFlags.DifferentColorPerNode);
        PrintFormat(Formatter.FormatFlags.DifferentColorPerNode | Formatter.FormatFlags.ParensAroundBinaryOperators);
        PrintFormat(Formatter.FormatFlags.DifferentColorPerNode | Formatter.FormatFlags.Indented);
        PrintFormat(Formatter.FormatFlags.DifferentColorPerNode | Formatter.FormatFlags.Indented | Formatter.FormatFlags.ParensAroundBinaryOperators);
    }
    [TestCaseSource("Cases")]
    public void Parse(string input, string expectedFormat, float? result = null)
    {
        INode parsed = Parser.Parse(input, out var err);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.Log(err);
        }

        var format = Formatter.Format(parsed, Formatter.FormatFlags.ParensAroundBinaryOperators);
        Debug.Log(format);
        Assert.AreEqual(expectedFormat, format);
        if (result.HasValue)
            Assert.AreEqual(result.Value, Evaluator.Eval(parsed, new Dictionary<string, float> { { "a", 7f } }));
    }

    [TestCase("32+4", "32 + 4")]
    [TestCase("32+ 4", "32 + 4")]
    [TestCase("32+ 4*1", "32 + 4 * 1")]
    [TestCase("32+ 4*a+2", "32 + 4 * a + 2")]
    [TestCase("1*a", "1 * a")]
    [TestCase("(32+4)", "( 32 + 4 )")]
    [TestCase("(32+4)*1", "( 32 + 4 ) * 1")]
    // [TestCase("1,2", "1 , 2")]
    public void Tokenizer_Works(string input, string spaceSeparatedTokens)
    {
        var reader = new Reader(input);
        string result = null;
        while (!reader.Done)
        {
            reader.ReadToken();
            var readerCurrentToken = reader.CurrentTokenType == Token.LeftParens ? "(" :
                reader.CurrentTokenType == Token.RightParens ? ")" : reader.CurrentToken;
            if (result == null)
                result = readerCurrentToken;
            else
                result += " " + readerCurrentToken;
        }

        Console.WriteLine(result);
        Assert.AreEqual(spaceSeparatedTokens, result);
    }
}

public static class Evaluator
{
    public static float Eval(INode node, Dictionary<string, float> variables = null)
    {
        switch (node)
        {
            case ExpressionValue v:
                return v.F;
            case Variable variable:
                return variables[variable.Id];
            case UnOp u:
                return u.Type == OpType.Plus ? Eval(u.A, variables) : -Eval(u.A, variables);
            case BinOp bin:
                var a = Eval(bin.A, variables);
                var b = Eval(bin.B, variables);
                switch (bin.Type)
                {
                    case OpType.Add:
                        return a + b;
                    case OpType.Sub:
                        return a - b;
                    case OpType.Mul:
                        return a * b;
                    case OpType.Div:
                        return a / b;
                    case OpType.Mod:
                        return a % b;
                    default:
                        throw new ArgumentOutOfRangeException(bin.Type.ToString());
                }
            case FuncCall f:
                void CheckArgCount(int n) => Assert.AreEqual(f.Arguments.Count, n);
                switch (f.Id)
                {
                    case "tan": return math.tan(Eval(f.Arguments.Single(), variables));
                    case "sin": return math.sin(Eval(f.Arguments.Single(), variables));
                    case "cos": return math.sin(Eval(f.Arguments.Single(), variables));
                    case "sqrt": return math.sqrt(Eval(f.Arguments.Single(), variables));
                    case "abs": return math.abs(Eval(f.Arguments.Single(), variables));
                    case "pow":
                        CheckArgCount(2);
                        return math.pow(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                    case "min":
                        CheckArgCount(2);
                        return math.min(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                    case "max":
                        CheckArgCount(2);
                        return math.max(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                    default: throw new InvalidDataException($"Unknown function {f.Id}");
                }

            default: throw new NotImplementedException();
        }
    }
}