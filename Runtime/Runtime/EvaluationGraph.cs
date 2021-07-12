using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace BurstExpressions.Runtime.Runtime
{
    public struct EvaluationGraph : IDisposable
    {
        public static unsafe uint4 Hash(Node[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
                return uint4.zero;
            fixed (Node* p = nodes)
                return xxHash3.Hash128(p, UnsafeUtility.SizeOf<Node>() * nodes.Length);
        }

        [NativeDisableUnsafePtrRestriction]
        public unsafe Node* Nodes;
        public ushort Length;
        public byte ExpectedFinalStackSize, MaxStackSize, ParameterCount;
        private Allocator _allocator;


        public unsafe EvaluationGraph(Node[] nodes, byte expectedFinalStackSize, byte maxStackSize, byte parameterCount, Allocator allocator = Allocator.Persistent)
        {
            var size = (ushort)(UnsafeUtility.SizeOf<Node>() * nodes.Length);
            Length = (ushort)nodes.Length;
            ExpectedFinalStackSize = expectedFinalStackSize;
            MaxStackSize = maxStackSize;
            ParameterCount = parameterCount;
            _allocator = allocator;
            Nodes = (Node*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<Node>(),
                _allocator);
            fixed (Node* ptr = nodes)
                UnsafeUtility.MemCpy(Nodes, ptr, size);
        }

        public unsafe void Dispose()
        {
            if (Nodes != null)
                UnsafeUtility.Free(Nodes, _allocator);
        }
    }

    [Serializable]
    public struct Node
    {
        public ushort Op;
        public float3 Val;
        public byte Index;

        public Node(EvalOp op, float3 val = default)
        {
            Assert.AreNotEqual(EvalOp.Param_0, op);
            Op = (ushort)op;
            Val = val;
            Index = 0;
        }
        public Node(ushort op, int mask, float3 val = default)
        {
            Op = (ushort)(op | (ushort)(mask << 12));
            Val = val;
            Index = 0;
        }

        public static Node Param(byte index)
        {
            Assert.AreNotEqual((byte)0, index, "Index must be in base1");
            return new Node(EvalOp.Param_0, index);
        }

        public static Node Ld(byte index)
        {
            Assert.AreNotEqual((byte)0, index, "Index must be in base1");
            return new Node(EvalOp.Ld_0, index);
        }

        private Node(EvalOp op, byte index)
        {
            Op = (ushort)op;
            Val = default;
            Index = index;
        }

        public override string ToString()
        {
            return $"{nameof(Op)}: {Op}, {nameof(Val)}: {Val}, {nameof(Index)}: {Index}";
        }
    }

    public static class Fbm
    {
        public static float fbm(float3 pos, float persistence, int octaves, float lacunarity)
        {
            float g = math.exp2(-persistence);
            float f = 1.0f;
            float a = 1.0f;
            float t = 0.0f;
            for (int i = 0; i < octaves; i++)
            {
                t += a * noise.snoise(f * pos);
                f *= lacunarity;
                a *= g;
            }

            return t;
        }
    }
}