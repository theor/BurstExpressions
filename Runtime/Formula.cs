using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eval.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Eval
{
    /// <summary>
    /// The class to store in a monobehaviour. Minimal sample:
    /// <code>
    /// public class FormulaTest : MonoBehaviour
    /// {
    ///     public Formula Test;
    ///     private EvalGraph _evalgraph;
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
    ///           EvalState.Run(_evalgraph, &t, out float3 res);
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
        private const byte MaxStackSize = 10;
        [SerializeField] internal EvalGraph.Node[] Content;
        public List<FormulaParam> NamedValues;
        public List<string> Params;

        public void SetDirty() => _dirty = true;

        internal string _error;
        private int _lastFormulaHashCode;
        internal bool _dirty;


        public delegate void FormulaChangedCallback(EvalGraph oldGraph, EvalGraph newGraph);
        [Conditional("UNITY_EDITOR")]
        public void LiveEdit(ref EvalGraph evalGraph, FormulaChangedCallback onFormulaChanged = null)
        {
            if (_dirty)
            {
                _dirty = false;
                Init();
                _lastFormulaHashCode = Input?.GetHashCode() ?? 0;
                EvalGraph oldGraph = evalGraph;
                if (Content == null)
                {
                    onFormulaChanged?.Invoke(oldGraph, default);
                    oldGraph.Dispose();
                    return;
                }


                evalGraph = new EvalGraph(Content, (byte)ExpectedFinalStackLength, MaxStackSize);
                onFormulaChanged?.Invoke(oldGraph, evalGraph);
                oldGraph.Dispose();
            }
        }


        public void Compile(out EvalGraph evalGraph)
        {
            if (Content == null || ExpectedFinalStackLength == 0)
                Init();

            // fixed (void* vptr = parsed)
            // {
            //     byte* bptr = (byte*) vptr;
            //     var byteLength = UnsafeUtility.SizeOf<EvalGraph.Node>() * parsed.Length;
            //     Content = new byte[byteLength];
            // }

            _lastFormulaHashCode = Input?.GetHashCode() ?? 0;

            evalGraph = new EvalGraph(Content, (byte)ExpectedFinalStackLength, MaxStackSize);
        }

        public void Init()
        {
            bool cleanup =
                    false
#if UNITY_EDITOR
                    || !UnityEditor.EditorApplication.isPlaying
#endif
                ;


            var root = Parser.Parse(Input, out _error);
            // Debug.Log($"PARSING cleanup={cleanup} error={_error}");
            if (_error != null)
            {
                Content = null;
                return;
            }
            Translator.Variables v = null;
            EvalGraph.Node[] parsed = null;
            if (root != null)
            {
                if (NamedValues != null)
                {
                    for (var index = 0; index < NamedValues.Count; index++)
                    {
                        var formulaParam = NamedValues[index];
                        if (formulaParam.IsSingleFloat == FormulaParam.FormulaParamFlag.Formula &&
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

    [Serializable]
    public struct FormulaParam
    {
        public enum FormulaParamFlag
        {
            Vector3,
            Float,
            Formula,
        }
        public string Name;
        public Vector3 Value;
        public FormulaParamFlag IsSingleFloat;
        [Delayed]
        public string SubFormula;
        public string SubFormulaError { get; private set; }
        public INode SubFormulaNode { get; private set; }

        public static FormulaParam FromSubFormula(string name, INode subformula)
        {
            return new FormulaParam(name, FormulaParamFlag.Formula) { SubFormulaNode = subformula };
        }

        public void ParseSubFormula()
        {
            SubFormulaNode = Parser.Parse(SubFormula, out var error);
            SubFormulaError = error;
        }

        public FormulaParam(string name, FormulaParamFlag isSingleFloat = FormulaParamFlag.Vector3)
        {
            Name = name;
            Value = default;
            IsSingleFloat = isSingleFloat;
            SubFormula = null;
            SubFormulaNode = null;
            SubFormulaError = null;
        }
    }
}