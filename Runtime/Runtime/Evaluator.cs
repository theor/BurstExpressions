using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace BurstExpressions.Runtime.Runtime
{
    public interface IOperators
    {
        void ExecuteOp<TContext>(in EvaluationInstruction instr, ref TContext impl) where TContext : struct, IContext;
    }
    public interface IContext
    {
        float3 Param(byte paramIndex);
        float3 Load(byte paramIndex);
        float3 Pop();
        void Push(float3 val);
    }

    [BurstCompile]
    public struct Evaluator
    {
        [BurstCompile]
        public static unsafe void Run2(in EvaluationGraph graph, float3* @params, int parameterCount, out float3 res)
        {
            res = new Evaluator().Run1(graph, default(DefaultOps), @params, parameterCount);
        }

        public static unsafe void Run3(in EvaluationGraph graph, NativeArray<float3> @params, out float3 res)
        {
            res = new Evaluator().Run1(graph, default(DefaultOps), (float3*)@params.GetUnsafeReadOnlyPtr(), @params.Length);
        }


        public static unsafe void Run4(in EvaluationGraph graph, float3[] @params, out float3 res)
        {
            fixed (float3* ptr = @params)
                res = new Evaluator().Run1(graph, default(DefaultOps), ptr, @params.Length);
        }

        [BurstCompile]
        public static unsafe void Run5(in EvaluationGraph graph, in float3 singleParam, out float3 res)
        {
            res = 0;
            // var p2 = singleParam;
            // RunOp(graph, &p2, 1, out res, default(DefaultOps));
        }


        // [BurstCompile]
        // public static unsafe void Run<TOperators>(in EvaluationGraph graph, float3* @params, int parameterCount, out float3 res, TOperators ops) where TOperators : struct, IOperators
        // {
        //     res = new Evaluator().Run(graph, ops, @params, parameterCount);
        // }
        //
        // public static unsafe void Run<TOperators>(in EvaluationGraph graph, NativeArray<float3> @params, out float3 res, TOperators ops) where TOperators : struct, IOperators
        // {
        //     res = new Evaluator().Run(graph, ops, (float3*)@params.GetUnsafeReadOnlyPtr(), @params.Length);
        // }
        //
        //
        // public static unsafe void Run<TOperators>(in EvaluationGraph graph, float3[] @params, out float3 res, TOperators ops) where TOperators : struct, IOperators
        // {
        //     fixed (float3* ptr = @params)
        //         res = new Evaluator().Run(graph, ops, ptr, @params.Length);
        // }
        //
        // [BurstCompile]
        // public static unsafe void RunOp<TOperators>(in EvaluationGraph graph, in float3 singleParam, out float3 res, TOperators ops) where TOperators : struct, IOperators
        // {
        //     var p2 = singleParam;
        //     Run(graph, &p2, 1, out res, ops);
        // }

        struct Impl : IContext
        {
            public UnsafeList<float3> Stack;
            private unsafe float3* _params;

            public unsafe Impl(UnsafeList<float3> stack, float3* @params)
            {
                Stack = stack;
                _params = @params;
            }

            public unsafe float3 Param(byte paramIndex)
            {
                return _params[paramIndex];
            }
            public float3 Load(byte paramIndex)
            {
                return Stack[paramIndex];
            }

            public float3 Pop()
            {
                var elt = Stack[--Stack.Length];
                return elt;
            }

            public void Push(float3 val)
            {
                Stack[Stack.Length++] = val;
                // Stack.AddNoResize(val);
            }
        }

        [BurstCompile]
        public unsafe float3 Run1<TOperators>(in EvaluationGraph graph, in TOperators operators, float3* @params, int parameterCount) where TOperators : struct, IOperators
        {
            if (graph.Length == 0)
                return default;
            Assert.AreEqual(parameterCount, graph.ParameterCount);
            using (var stack = new UnsafeList<float3>(graph.MaxStackSize, Allocator.Temp))
            {
                Impl impl = new Impl(stack, @params);
                impl.Stack.Clear();
                for (int current = 0; current < graph.Length; current++)
                {
                    var node = graph.Nodes[current];
                    operators.ExecuteOp(node, ref impl);
                }

                Assert.AreNotEqual(0, impl.Stack.Length);
                Assert.AreEqual(graph.ExpectedFinalStackSize, impl.Stack.Length);
                return impl.Stack[impl.Stack.Length - 1];
            }
        }

        /*
         * 1 + 2 * 3
         * 1 2 3 * + 
         * 1 6 +
         * 7
         * x + 2 * 3 
         * x 2 3 * +
         * x 6 +
         * 1 * x + 2 * 3
         * 1 x * 2 3 * +
         * 1 x * 6 +
         */

        public struct DefaultOps : IOperators
        {
            public void ExecuteOp<TContext>(in EvaluationInstruction instr, ref TContext impl) where TContext : struct, IContext
            {
                switch (instr.Op)
                {
                    // unary
                    case EvalOp.Minus_1:
                        impl.Push(-impl.Pop());
                        break;
                    // no params
                    case EvalOp.Const_0:
                        impl.Push(instr.Val);
                        break;
                    case EvalOp.Param_0:
                        impl.Push(impl.Param((byte)(instr.Index - 1)));
                        break;
                    case EvalOp.Ld_0:
                        impl.Push(impl.Load((byte)(instr.Index - 1)));
                        break;

                    // binary and more
                    case EvalOp.Add_2:
                        impl.Push(impl.Pop() + impl.Pop());
                        break;
                    case EvalOp.Sub_2:
                        impl.Push(impl.Pop() - impl.Pop());
                        break;
                    case EvalOp.Div_2:
                        impl.Push(impl.Pop() / impl.Pop());
                        break;
                    case EvalOp.Mul_2:
                        impl.Push(impl.Pop() * impl.Pop());
                        break;
                    case EvalOp.Mod_2:
                        impl.Push(math.fmod(impl.Pop(), impl.Pop()));
                        break;
                    case EvalOp.X_1:
                        impl.Push(impl.Pop().x);
                        break;
                    case EvalOp.Y_1:
                        impl.Push(impl.Pop().y);
                        break;
                    case EvalOp.Z_1:
                        impl.Push(impl.Pop().z);
                        break;
                    case EvalOp.Sin_1:
                        impl.Push(math.sin(impl.Pop()));
                        break;
                    case EvalOp.Cos_1:
                        impl.Push(math.cos(impl.Pop()));
                        break;
                    case EvalOp.Pow_2:
                        impl.Push(math.pow(impl.Pop(), impl.Pop()));
                        break;
                    case EvalOp.Sqrt_1:
                        impl.Push(math.sqrt(impl.Pop()));
                        break;
                    case EvalOp.Abs_1:
                        impl.Push(math.abs(impl.Pop()));
                        break;
                    case EvalOp.Saturate_1:
                        impl.Push(math.saturate(impl.Pop()));
                        break;
                    case EvalOp.Tan_1:
                        impl.Push(math.tan(impl.Pop()));
                        break;
                    case EvalOp.Dist_2:
                        impl.Push(math.distance(impl.Pop(), impl.Pop()));
                        break;
                    case EvalOp.SqDist_2:
                        impl.Push(math.distancesq(impl.Pop(), impl.Pop()));
                        break;
                    case EvalOp.Fbm_1:
                        impl.Push(Fbm.fbm(impl.Pop(), 1, 5, 0.4f));
                        break;
                    case EvalOp.Fbm_4:
                        impl.Push(Fbm.fbm(impl.Pop(), impl.Pop().x, (int)impl.Pop().x, impl.Pop().x));
                        break;
                    case EvalOp.CNoise_1:
                        impl.Push(noise.cnoise(impl.Pop()));
                        break;
                    case EvalOp.SNoise_1:
                        impl.Push(noise.snoise(impl.Pop()));
                        break;
                    case EvalOp.SRDNoise_1:
                        var float3 = impl.Pop();
                        impl.Push(noise.srdnoise(float3.xy, float3.z));
                        break;
                    case EvalOp.V3_3:
                        impl.Push(new float3(impl.Pop().x, impl.Pop().x, impl.Pop().x));
                        break;
                    case EvalOp.Box_2:
                        {
                            var p = impl.Pop();
                            var b = impl.Pop();
                            var q = math.abs(p) - b;
                            impl.Push(math.length(math.max(q, 0)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0));
                            break;
                        }
                    case EvalOp.Clamp_3:
                        impl.Push(math.clamp(impl.Pop(), impl.Pop(), impl.Pop()));
                        break;
                    // boolean
                    case EvalOp.Gt_2:
                        impl.Push(FromBool3(impl.Pop() > impl.Pop()));
                        break;
                    case EvalOp.Gte_2:
                        impl.Push(FromBool3(impl.Pop() >= impl.Pop()));
                        break;
                    case EvalOp.Lt_2:
                        impl.Push(FromBool3(impl.Pop() < impl.Pop()));
                        break;
                    case EvalOp.Lte_2:
                        impl.Push(FromBool3(impl.Pop() <= impl.Pop()));
                        break;
                    case EvalOp.Select_3:
                        {
                            var cond = impl.Pop();
                            var a = impl.Pop();
                            var b = impl.Pop();
                            impl.Push(math.@select(b, a, Bool3(cond)));
                            break;
                        }
                    default:
                        throw new NotImplementedException(string.Format("Operator {0} is not implemented", instr.Op));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool Bool(float3 f) => f.x != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool3 Bool3(float3 f) => new bool3(Bool(f.x), Bool(f.y), Bool(f.z));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            float3 FromBool(bool b) => b ? 1 : 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            float3 FromBool3(bool3 b) => math.all(b) ? 1 : 0;
        }
    }
}