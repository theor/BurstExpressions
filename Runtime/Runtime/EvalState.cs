using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Eval.Runtime
{
    public interface IContext
    {
        float3 Param(byte paramIndex);
        float3 Load(byte paramIndex);
        float3 Pop();
        void Push(float3 val);
    }

    [BurstCompile]
    public struct EvalState
    {
        private NativeList<float3> _stack;
        
        private int _current;
        

        [BurstCompile]
        public static unsafe void Run(in EvalGraph graph, float3* @params, out float3 res)
        {
            res = new EvalState().Run(graph, @params);
        }

        public static unsafe void Run(in EvalGraph graph, NativeArray<float3> @params, out float3 res)
        {
            res = new EvalState().Run(graph, (float3*) @params.GetUnsafeReadOnlyPtr());
        }

        
        public static unsafe void Run(in EvalGraph graph, float3[] @params, out float3 res)
        {
            fixed(float3* ptr = @params)
                res = new EvalState().Run(graph, ptr);
        }

        [BurstCompile]
        public static unsafe void Run(in EvalGraph graph, in float3 singleParam, out float3 res)
        {
            var p2 = singleParam;
            Run(graph, &p2, out res);
        }

        struct Impl : IContext
        {
            private NativeList<float3> _stack;
            private unsafe float3* _params;

            public unsafe Impl(NativeList<float3> stack, float3* @params)
            {
                _stack = stack;
                _params = @params;
            }

            public unsafe float3 Param(byte paramIndex)
            {
                return _params[paramIndex];
            }
            public float3 Load(byte paramIndex)
            {
                return _stack[paramIndex];
            }

            public float3 Pop()
            {
                var elt = _stack[_stack.Length - 1];
                _stack.RemoveAt(_stack.Length-1);
                return elt;
            }

            public void Push(float3 val)
            {
                _stack.Add(val);
            }
        }
        
        [BurstCompile]
        public unsafe float3 Run(in EvalGraph graph,  float3* @params)
        {
            using (_stack = new NativeList<float3>(graph.MaxStackSize, Allocator.Temp))
            {
                Impl impl = new Impl(_stack, @params);
                _current = 0;
                _stack.Clear();
                while (_current < graph.Length)
                {
                    var node = graph.Nodes[_current];
                    ExecuteOp(node, ref impl);

                    _current++;
                }

                Assert.AreNotEqual(0, _stack.Length);
                Assert.AreEqual(graph.ExpectedFinalStackSize, _stack.Length);
                return _stack[_stack.Length-1];
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
        public static void ExecuteOp<TContext>(in EvalGraph.Node node, ref TContext impl) where TContext:struct, IContext
        {
            switch (node.Op)
            {
                // unary
                case EvalOp.Minus_1:
                    impl.Push(-impl.Pop());
                    break;
                // no params
                case EvalOp.Const_0:
                    impl.Push(node.Val);
                    break;
                case EvalOp.Param_0:
                    impl.Push(impl.Param((byte) (node.Index - 1)));
                    break;
                case EvalOp.Ld_0:
                    impl.Push(impl.Load((byte)(node.Index - 1)));
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
                    impl.Push(Fbm.fbm(impl.Pop(), impl.Pop().x, (int) impl.Pop().x, impl.Pop().x));
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
                    var p = impl.Pop();
                    var b = impl.Pop();
                    var q = math.abs(p) - b;
                    impl.Push(math.length(math.max(q, 0)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0));
                    break;
                default:
                    throw new NotImplementedException(string.Format("Operator {0} is not implemented", node.Op));
            }
        }
    }
}