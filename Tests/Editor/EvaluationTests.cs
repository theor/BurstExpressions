using System;
using System.Reflection;
using System.Reflection.Emit;
using BurstExpressions.Runtime.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class JitTests : EvaluationTestsBase
{
    [Test]
    public void COmpile()
    {

        var nodes = new[] { new EvaluationInstruction(EvalOp.Const_0, new float3(1, 2, 3)) };
        var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("FormulaTest"), AssemblyBuilderAccess.RunAndSave);
        var module = asm.DefineDynamicModule("FormulaTestModule", "formulatest.dll", true);
        var type = module.DefineType("Formulas", TypeAttributes.Public |
                                                 TypeAttributes.Class |
                                                 TypeAttributes.AutoClass |
                                                 TypeAttributes.AnsiClass |
                                                 TypeAttributes.BeforeFieldInit |
                                                 TypeAttributes.AutoLayout);
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(BurstCompileAttribute).GetConstructor(new Type[0]), new object[0]));
        var method = type.DefineMethod("Formula", MethodAttributes.Public | MethodAttributes.Static, typeof(float3), Type.EmptyTypes);

        method.SetCustomAttribute(new CustomAttributeBuilder(typeof(BurstCompileAttribute).GetConstructor(new Type[0]), new object[0]));

        var float3Ctor = typeof(float3).GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });

        var il = method.GetILGenerator();

        // il.Emit(OpCodes.Ldloca_S, loc.LocalIndex);
        // il.Emit(OpCodes.Initobj, typeof(float3));
        il.Emit(OpCodes.Ldc_R4, 1f);
        il.Emit(OpCodes.Ldc_R4, 2f);
        il.Emit(OpCodes.Ldc_R4, 3f);
        il.Emit(OpCodes.Newobj, float3Ctor);

        il.Emit(OpCodes.Ldc_R4, 4f);
        il.Emit(OpCodes.Ldc_R4, 5f);
        il.Emit(OpCodes.Ldc_R4, 6f);
        il.Emit(OpCodes.Newobj, float3Ctor);

        il.EmitCall(OpCodes.Call, typeof(float3).GetMethod("op_Addition", new[] { typeof(float3), typeof(float3) }), Type.EmptyTypes);
        // il.Emit(OpCodes.Ldloc, loc.LocalIndex);
        il.Emit(OpCodes.Ret);

        var t = type.CreateType();

        asm.Save("formulatest.dll");

        var m = t.GetMethod("Formula");
        var res = m.Invoke(null, null);
        Debug.Log(res);
    }
}
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
            Results[index] = state.Run1(EvaluationGraph, new Evaluator.DefaultOps(), (float3*)nativeSlice.GetUnsafeReadOnlyPtr(), nativeSlice.Length);
        }
    }

    [Test]
    public void ConstFloat3() =>
        Run(new float3(1, 2, 3), new[] { new EvaluationInstruction(EvalOp.Const_0, new float3(1, 2, 3)) }, 1, 10);

    [Test]
    public void Test_LD() =>
        Run(new float3(.5f), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(5)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(10)),
            EvaluationInstruction.Ld(2),
            EvaluationInstruction.Ld(1),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 3, 10);

    [Test]
    public void Test_LD2() =>
        Run(new float3(.5f), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(10)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(5)),
            // 5 / 10
            EvaluationInstruction.Ld(1),
            EvaluationInstruction.Ld(2),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 3, 10);

    [Test]
    public void Params() =>
        Run(new float3(1, 2, 3), new[]
        {
            EvaluationInstruction.Param(1),
            EvaluationInstruction.Param(2),
            new EvaluationInstruction(EvalOp.Add_2),
        }, 1, 10, new float3(1, 2, 0), new float3(0, 0, 3));

    [Test]
    public void AddFloat3() =>
        Run(new float3(5, 7, 9), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, new float3(1, 2, 3)),
            new EvaluationInstruction(EvalOp.Const_0, new float3(4, 5, 6)),
            new EvaluationInstruction(EvalOp.Add_2),
        }, 1, 10);

    [Test]
    public void Div() =>
        Run(new float3(2), new[]
        {
            new EvaluationInstruction(EvalOp.Const_0, 3f),
            new EvaluationInstruction(EvalOp.Const_0, 6f),
            new EvaluationInstruction(EvalOp.Div_2),
        }, 1, 10);
}