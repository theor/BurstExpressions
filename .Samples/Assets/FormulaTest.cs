using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Runtime;
using Unity.Mathematics;
using UnityEngine;

public class FormulaTest : MonoBehaviour
{
    public Formula Test;
    private EvaluationGraph _evalgraph;
    public bool LiveEdit;

    public void Awake() => Debug.Log("Awake");
    public void OnEnable() => Debug.Log("OnEnable");

    public void Reset()
    {
        if (Test == null) Test = new Formula();
        Debug.Log("Set params");
        Test.SetParameters("t", "pos");
    }

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
        if (LiveEdit)
            Test.LiveEdit(ref _evalgraph);

        var parameters = new float3[2];
        parameters[0] = Time.realtimeSinceStartup;
        parameters[1] = transform.localPosition;
        float3 res = float3.zero;
        // Stopwatch sw = Stopwatch.StartNew();
        // for (int i = 0; i < 100000; i++)
        {
            // res = new float3(math.cos(t * 7), math.sin(t * 7), 0);
            Evaluator.Run(_evalgraph, parameters, out res);
        }

        // var ms = sw.ElapsedMilliseconds;
        // Debug.Log($"{ms}ms");
        transform.localPosition = res;
    }
}
