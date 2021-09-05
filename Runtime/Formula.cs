using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Runtime;
using UnityEngine;

namespace BurstExpressions.Runtime
{
    /// <summary>
    /// The class to store in a monobehaviour. Minimal sample:
    /// <code>
    /// public class FormulaTest : MonoBehaviour
    /// {
    ///     public Formula Test;
    ///     private EvaluationGraph _evalgraph;
    ///   
    ///     public void Reset()
    ///     {
    ///         if (Test == null) Test = new Formula();
    ///         Test.SetParameters("t");
    ///     }
    ///   
    ///     private void Start() => Test.Compile(out _evalgraph);
    ///   
    ///       private void OnDestroy() =>_evalgraph.Dispose();
    ///   
    ///       private unsafe void Update()
    ///       {
    ///           #if UNITY_EDITOR
    ///           Test.LiveEdit(ref _evalgraph);
    ///           #endif
    ///           float3 t = Time.realtimeSinceStartup;
    ///           Evaluator.Run(_evalgraph, &t, out float3 res);
    ///           transform.localPosition = res;
    ///       }
    /// }
    /// </code>
    /// </summary>
    [Serializable]
    public class Formula
    {

        [Delayed]
        public string Input;

        public Translator.TranslationOptions Options;

        [SerializeField]
        private int ExpectedFinalStackLength;

        [SerializeField] public int MaxStackSize = 10;

        [SerializeField] internal EvaluationInstruction[] Content;
        public List<NamedValue> NamedValues;
        public List<string> Params;

        public void SetDirty() => _dirty = true;

        internal string _error;
        private int _lastFormulaHashCode;
        internal bool _dirty;


        public delegate void FormulaChangedCallback(EvaluationGraph oldGraph, EvaluationGraph newGraph);
        [Conditional("UNITY_EDITOR")]
        public void LiveEdit(ref EvaluationGraph evaluationGraph, FormulaChangedCallback onFormulaChanged = null)
        {
            if (_dirty)
            {
                _dirty = false;
                Init();
                _lastFormulaHashCode = Input?.GetHashCode() ?? 0;
                EvaluationGraph oldGraph = evaluationGraph;
                if (Content == null || _error != null)
                {
                    onFormulaChanged?.Invoke(oldGraph, default);
                    oldGraph.Dispose();
                    evaluationGraph = default;
                    return;
                }


                evaluationGraph = new EvaluationGraph(Content, (byte)ExpectedFinalStackLength, (byte)MaxStackSize, (byte)Params.Count);
                onFormulaChanged?.Invoke(oldGraph, evaluationGraph);
                oldGraph.Dispose();
            }
        }


        public void Compile(out EvaluationGraph evaluationGraph)
        {
            if (Content == null || ExpectedFinalStackLength == 0)
                Init();

            _lastFormulaHashCode = Input?.GetHashCode() ?? 0;

            evaluationGraph = new EvaluationGraph(Content, (byte)ExpectedFinalStackLength, (byte)MaxStackSize, (byte)Params.Count);
        }

        public void Init()
        {
            bool cleanup =
#if UNITY_EDITOR
                    !UnityEditor.EditorApplication.isPlaying
#else
                    false
#endif
                ;


            if (!Parser.TryParse(Input, out var root, out var error))
            {
                _error = error.ToString();
                Content = null;
                return;
            }

            _error = null;
            // Debug.Log($"PARSING cleanup={cleanup} error={_error}");
            Translator.Variables v = null;
            EvaluationInstruction[] parsed = null;
            if (root != null)
            {
                if (NamedValues != null)
                {
                    for (var index = 0; index < NamedValues.Count; index++)
                    {
                        var formulaParam = NamedValues[index];
                        if (formulaParam.IsSingleFloat == NamedValue.FormulaParamFlag.Formula &&
                            formulaParam.SubFormulaNode == null && string.IsNullOrEmpty(formulaParam.SubFormulaError))
                        {
                            formulaParam.ParseSubFormula();
                            NamedValues[index] = formulaParam;
                        }
                    }
                }
                try
                {
                    parsed = Translator.Translate(root, NamedValues, Params, out v, Options);

                }
                catch (Exception e)
                {
                    _error = e.Message;
                    return;
                }
            }
            if (cleanup && NamedValues != null)
            {
                for (var index = NamedValues.Count - 1; index >= 0; index--)
                    if (v != null && !v.VariableInfos.TryGetValue(NamedValues[index].Name, out var info))
                        NamedValues.RemoveAt(index);
            }

            ExpectedFinalStackLength = v.NextIndex;
            MaxStackSize = v.MaxStackSize;
            Content = parsed;
        }

        public void SetParameters(params string[] formulaParams)
        {
            if (Params == null)
                Params = formulaParams.ToList();
            else
            {
                Params.Clear();
                Params.AddRange(formulaParams);
            }
        }
    }
}