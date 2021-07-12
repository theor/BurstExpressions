using System;
using System.Configuration;
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
            var defaultOps = new Evaluator.DefaultOps();
            Results[index] = state.Run(EvaluationGraph, ref defaultOps, (float3*)nativeSlice.GetUnsafeReadOnlyPtr(), nativeSlice.Length);
        }
    }

    [BurstCompile]
    public struct EvaluationSwitcherJob : IJob
    {
        public EvaluationGraph EvaluationGraph;
        public NativeReference<float3> Result;

        public unsafe void Execute()
        {
            Evaluator state = new Evaluator();
            var defaultOps = new Switcher<Evaluator.DefaultOps, Switcher<ExtensionOperators, LastSwitcher>>();
            Result.Value = state.Run(EvaluationGraph, ref defaultOps, null, 0);
        }
    }

    enum ExtensionOps : ushort
    {
        None,
        Times2,
    }
    struct ExtensionOperators : IOperators
    {
        public void ExecuteOp<TContext>(in Node node, ref TContext impl) where TContext : struct, IContext
        {
            switch ((ExtensionOps)node.Op)
            {
                // unary
                case ExtensionOps.Times2:
                    impl.Push(2 * impl.Pop());
                    break;
                default:
                    throw new NotImplementedException(string.Format("Operator {0} is not implemented", (ExtensionOps)node.Op));
            }
        }

    }

    struct LastSwitcher : IOperators
    {
        public void ExecuteOp<TContext>(in Node node, ref TContext impl) where TContext : struct, IContext
        {
            throw new NotImplementedException();
        }
    }

    struct Switcher<T1, T2> : IOperators where T1 : struct, IOperators where T2 : struct, IOperators
    {
        public void ExecuteOp<TContext>(in Node node, ref TContext impl) where TContext : struct, IContext
        {
            var opMask = (node.Op & 0xF000) >> 12;
            if (opMask == 0)
                default(T1).ExecuteOp(node, ref impl);
            else
            {
                Node copy = node;
                copy.Op = (ushort)((node.Op & 0x0FFF) | ((opMask - 1) << 12));
                default(T2).ExecuteOp(copy, ref impl);
            }
        }
    }

    static unsafe float3 RunSwitcher(params Node[] nodes)
    {
        Evaluator state = new Evaluator();
        EvaluationGraph graph = new EvaluationGraph(nodes, 1, 2, 0, Allocator.Temp);
        var job = new EvaluationSwitcherJob
        {
            EvaluationGraph = graph,
            Result = new NativeReference<float3>(Allocator.TempJob)
        };
        job.Run();
        return job.Result.Value;
    }

    // A Test behaves as an ordinary method
    [Test]
    public unsafe void Ext()
    {
        var res = RunSwitcher(new Node(EvalOp.Const_0, new float3(1, 2, 3)));
        Assert.AreEqual(new float3(1, 2, 3), res);
    }
    [Test]
    public unsafe void Ext2()
    {
        var res = RunSwitcher(
            new Node(EvalOp.Const_0, new float3(1, 2, 3)),
            new Node((ushort)ExtensionOps.Times2, 1)
        );
        Assert.AreEqual(new float3(2, 4, 6), res);
    }
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