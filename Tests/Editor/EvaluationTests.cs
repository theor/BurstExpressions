using Eval.Runtime;
using NUnit.Framework;
using Unity.Mathematics;

public class EvaluationTests : EvaluationTestsBase
{
    // A Test behaves as an ordinary method
    [Test]
    public void ConstFloat3()
    {
        Run(new float3(1, 2, 3), new[] { new Node(EvalOp.Const_0, new float3(1, 2, 3)) }, 1, 10);
    }
    [Test]
    public void Test_LD()
    {
        Run(new float3(.5f), new[]
        {
            new Node(EvalOp.Const_0, new float3(5)),
            new Node(EvalOp.Const_0, new float3(10)),
            Node.Ld(2),
            Node.Ld(1),
            new Node(EvalOp.Div_2),
        }, 3, 10);
    }
    [Test]
    public void Test_LD2()
    {
        Run(new float3(.5f), new[]
        {
            new Node(EvalOp.Const_0, new float3(10)),
            new Node(EvalOp.Const_0, new float3(5)),
            // 5 / 10
            Node.Ld(1),
            Node.Ld(2),
            new Node(EvalOp.Div_2),
        }, 3, 10);
    }

    [Test]
    public void Params()
    {
        Run(new float3(1, 2, 3), new[]
        {
            Node.Param(1),
            Node.Param(2),
            new Node(EvalOp.Add_2),
        }, 1, 10, new float3(1, 2, 0), new float3(0, 0, 3));
    }

    [Test]
    public void AddFloat3()
    {
        Run(new float3(5, 7, 9), new[]
        {
            new Node(EvalOp.Const_0, new float3(1, 2, 3)),
            new Node(EvalOp.Const_0, new float3(4, 5, 6)),
            new Node(EvalOp.Add_2),
        }, 1, 10);
    }

    [Test]
    public void Div()
    {
        Run(new float3(2), new[]
        {
            new Node(EvalOp.Const_0, 3f),
            new Node(EvalOp.Const_0, 6f),
            new Node(EvalOp.Div_2),
        }, 1, 10);
    }
}