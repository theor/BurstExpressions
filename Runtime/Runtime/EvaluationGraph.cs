using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace BurstExpressions.Runtime.Runtime
{
    public struct EvaluationGraph : IDisposable
    {
        public static unsafe uint4 Hash(EvaluationInstruction[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
                return uint4.zero;
            fixed (EvaluationInstruction* p = nodes)
                return xxHash3.Hash128(p, UnsafeUtility.SizeOf<EvaluationInstruction>() * nodes.Length);
        }

        [NativeDisableUnsafePtrRestriction]
        public unsafe EvaluationInstruction* Nodes;
        public ushort Length;
        public byte ExpectedFinalStackSize, MaxStackSize, ParameterCount;
        private Allocator _allocator;


        public unsafe EvaluationGraph(EvaluationInstruction[] nodes, byte expectedFinalStackSize, byte maxStackSize, byte parameterCount, Allocator allocator = Allocator.Persistent)
        {
            var size = (ushort)(UnsafeUtility.SizeOf<EvaluationInstruction>() * nodes.Length);
            Length = (ushort)nodes.Length;
            ExpectedFinalStackSize = expectedFinalStackSize;
            MaxStackSize = maxStackSize;
            ParameterCount = parameterCount;
            _allocator = allocator;
            Nodes = (EvaluationInstruction*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<EvaluationInstruction>(),
                _allocator);
            fixed (EvaluationInstruction* ptr = nodes)
                UnsafeUtility.MemCpy(Nodes, ptr, size);
        }

        public unsafe void Dispose()
        {
            if (Length > 0 && Nodes != null)
                UnsafeUtility.Free(Nodes, _allocator);
        }
    }

    [Serializable]
    public struct EvaluationInstruction
    {
        public EvalOp Op;
        public float3 Val;
        public byte Index;

        public EvaluationInstruction(EvalOp op, float3 val = default)
        {
            Assert.AreNotEqual(EvalOp.Param_0, op);
            Op = op;
            Val = val;
            Index = 0;
        }

        public static EvaluationInstruction Param(byte index)
        {
            Assert.AreNotEqual((byte)0, index, "Index must be in base1");
            return new EvaluationInstruction(EvalOp.Param_0, index);
        }

        public static EvaluationInstruction Ld(byte index)
        {
            Assert.AreNotEqual((byte)0, index, "Index must be in base1");
            return new EvaluationInstruction(EvalOp.Ld_0, index);
        }

        private EvaluationInstruction(EvalOp op, byte index)
        {
            Op = op;
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