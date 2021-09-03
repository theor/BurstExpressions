using System.Collections;
using System.Collections.Generic;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Runtime;
using UnityEngine;

public class Easing : MonoBehaviour
{
    public Formula Test;
    private EvaluationGraph _evalgraph;
    public void Reset()
    {
        if (Test == null) Test = new Formula();
        Test.SetParameters("x");
    }
    // Start is called before the first frame update
    private void Start()
    {
        Test.Compile(out _evalgraph);
    }

    private void OnDestroy()
    {
        _evalgraph.Dispose();
    }

    private void Update()
    {
        Test.LiveEdit(ref _evalgraph);

        var t = (Time.realtimeSinceStartup % 4) / 2;
        bool reversed = false;
        if (t > 1)
        {
            reversed = true;
            t -= 1;
        }

        Evaluator.Run(_evalgraph, Mathf.Clamp01(t), out var res);
        // Debug.Log($"{t:F2} {res:F1}");
        var transformLocalPosition = transform.localPosition;
        transformLocalPosition.y = reversed ? (1 - res.x) : res.x;
        transform.localPosition = transformLocalPosition;
    }
}
