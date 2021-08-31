using System;
using System.Collections;
using BurstExpressions.Runtime;
using BurstExpressions.Runtime.Runtime;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Playmode
{
    public class PlaymodeTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            var go = new GameObject();
            var f = go.AddComponent<TestFormulaMonoBehaviour>();
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
            yield return null;
            Assert.AreEqual(new float3(42), f.Result);
        }
    }

    public class TestFormulaMonoBehaviour : MonoBehaviour
    {
        public Formula Formula;
        public float3 Result;
        private EvaluationGraph _evaluationGraph;

        private void Start()
        {
            Formula = new Formula();
            Formula.SetParameters("t");
            Formula.Input = "t";
            Formula.Compile(out _evaluationGraph);
        }

        private void OnDestroy() => _evaluationGraph.Dispose();

        private void Update()
        {
            Evaluator.Run(_evaluationGraph, 42, out Result);
        }

    }
}