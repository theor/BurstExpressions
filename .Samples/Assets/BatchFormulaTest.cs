using System.Collections.Generic;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class BatchFormulaTest : MonoBehaviour
{
    public Formula Test;
    public int Count;
    public bool LiveEdit;
    public bool CompleteJobsImmediately;
    public bool UseRawCodeVersion;

    private EvaluationGraph _evalgraph;
    private Transform[] _instances;
    private NativeArray<float3> _results;
    private TransformAccessArray _transformAccessArray;
    private JobHandle _handle;

    public void Reset()
    {
        if (Test == null) Test = new Formula();
        Debug.Log("Set params");
        Test.SetParameters("t", "i");
    }

    private void Start()
    {
        Test.Compile(out _evalgraph);
        _instances = new Transform[Count];

        _results = new NativeArray<float3>(Count, Allocator.Persistent);
        for (int i = 0; i < Count; i++)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Component.Destroy(o.GetComponent<Collider>());
            _instances[i] = o.transform;
        }

        _transformAccessArray = new TransformAccessArray(_instances);
    }

    private void OnDestroy()
    {
        _handle.Complete();
        _results.Dispose();
        _evalgraph.Dispose();
    }

    [BurstCompile]
    struct SpeedOfLightJob : IJobParallelForTransform
    {
        public float Time;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = new float3(index, math.sin(Time * 14 + index) * 5, 0);
        }
    }

    [BurstCompile]
    struct TRJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> Results;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = Results[index];
        }
    }

    [BurstCompile]
    public struct CustomEvaluationJob : IJobParallelFor
    {
        public EvaluationGraph EvaluationGraph;
        public float3 Time;
        public NativeArray<float3> Results;

        public unsafe void Execute(int index)
        {
            Evaluator state = new Evaluator();
            float3* @params = stackalloc float3[2];
            @params[0] = Time;
            @params[1] = index;
            Results[index] = state.Run<Evaluator.DefaultOps>(EvaluationGraph, default, @params, 2);
        }
    }

    private unsafe void Update()
    {
        if (LiveEdit)
            Test.LiveEdit(ref _evalgraph);

        _handle.Complete();

        // Stopwatch sw = Stopwatch.StartNew();
        float time = Time.realtimeSinceStartup;

        if (UseRawCodeVersion)
        {
            _handle = new SpeedOfLightJob
            {
                Time = time,
            }.Schedule(_transformAccessArray);
        }
        else
        {
            var handle = new CustomEvaluationJob
            {
                Time = time,
                Results = _results,
                EvaluationGraph = _evalgraph,
            }.Schedule(Count, 32);

            _handle = new TRJob
            {
                Results = _results
            }.Schedule(_transformAccessArray, handle);
        }

        if (CompleteJobsImmediately)
            _handle.Complete();
    }
}