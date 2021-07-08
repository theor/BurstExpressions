# BurstExpressions

An optimizing  expression parser with an efficientBurst-compiled evaluation engine and live-edit in editor (soon, optionally in the runtime too).

When declaring a formula, you can specify the parameters it will receive from the code, like the current time `t`.

Any value that is not a parameter will be considered as a *named value*. A named value can be a single `float` or `float3` or a nested formula that can use the same parameters and named values and declare new named values.

## How it works

The parser (a standard [Shunting-Yard](https://en.wikipedia.org/wiki/Shunting-yard_algorithm) algorithm) creates an AST, which is then transformed to [Reverse Polish Notation](https://en.wikipedia.org/wiki/Reverse_Polish_notation). The RPN is stored as an array and evaluated using a simple [Stack machine](https://en.wikipedia.org/wiki/Stack_machine).

The RPN expression is optionally optimized using [Constant Folding](https://en.wikipedia.org/wiki/Constant_folding), which means that an expression like `2+3*4` will result in a constant `14`.

## Getting started

```csharp
using Eval;
using Eval.Runtime;
using Unity.Mathematics;
using UnityEngine;

public class FormulaTest : MonoBehaviour
{
    // The formula itself
    public Formula Test;
    private EvalGraph _evalgraph;

    public void Reset()
    {
        if (Test == null) Test = new Formula();
        Test.SetParameters("t", "pos");
    }

    // compile the formula once
    private void Start() => Test.Compile(out _evalgraph);

    private void OnDestroy() => _evalgraph.Dispose();

    private void Update()
    {
        // this call is editor only and will get stripped in a build
        Test.LiveEdit(ref _evalgraph);

        // parameters can be provided as a single float3 if you only need one, a NativeArray<float3>
        // or a float3*. If you use a pointer and have already allowed unsafe, using a stackalloc float3[2]
        // can be practical
        var parameters = new float3[2];
        parameters[0] = Time.realtimeSinceStartup;
        parameters[1] = transform.localPosition;
        
        float3 res = float3.zero;
        EvalState.Run(_evalgraph, parameters, out res);

        transform.localPosition = res;
    }
}
```