using System.Collections;
using System.Collections.Generic;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Runtime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

public class VFXFormula : MonoBehaviour
{
    public Formula Test;
    private EvaluationGraph _evalgraph;
    private VisualEffect _vfx;
    private ExposedProperty _initPositionId;

    public void Reset()
    {
        if (Test == null) Test = new Formula();
        Test.SetParameters("t");
    }

    // compile the formula once
    private void Start()
    {
        Test.Compile(out _evalgraph);
        _vfx = GetComponent<VisualEffect>();
        _initPositionId = "Initial Position";
    }

    private void OnDestroy() => _evalgraph.Dispose();

    private void Update()
    {
        Test.LiveEdit(ref _evalgraph);

        Evaluator.Run<Evaluator.DefaultOps>(_evalgraph, Time.realtimeSinceStartup, out var res);
        _vfx.SetVector3(_initPositionId, res);
    }
}

