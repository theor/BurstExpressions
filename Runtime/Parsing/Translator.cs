using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BurstExpressions.Runtime.Parsing.AST;
using BurstExpressions.Runtime.Runtime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace BurstExpressions.Runtime.Parsing
{
    /// <summary>
    /// Translates the AST to a flat array containing the reverse polish notation (RPN) of the expression
    /// if the expression is 1 * (2 + 3), the RPN is 1 2 3 + * 
    /// </summary>
    public static class Translator
    {
        private static FormulaParamNameComparer s_ParamNameComparer = new FormulaParamNameComparer();
        public static IComparer<NamedValue> FormulaParamsCompareByName => s_ParamNameComparer;

        public class VariableInfo
        {
            // Ld_0 op index, base 1
            public int Index;
            public List<EvaluationInstruction> Translated;
            // instead of computing the subformula once and loading the cached result with LD, recompute it everytime
            public bool Inline;
        }

        public class Variables
        {
            public int MaxStackSize;
            public int NextIndex = 1;
            internal int _currentStackSize;
            public Dictionary<string, VariableInfo> VariableInfos = new Dictionary<string, VariableInfo>();
        }

        internal class FormulaParamNameComparer : IComparer<NamedValue>
        {
            public int Compare(NamedValue x, NamedValue y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        [Flags]
        public enum TranslationOptions
        {
            None = 0,
            FoldConstantExpressions = 1,
        }
        public static EvaluationInstruction[] Translate(IAstNode node, List<NamedValue> variables, List<string> parameters,
            out Variables v, TranslationOptions options = TranslationOptions.None)
        {

            List<EvaluationInstruction> nodes = new List<EvaluationInstruction>();
            v = new Variables();
            Rec(nodes, variables, node, parameters, v, options);
            int insertIndex = 0;
            foreach (var keyValuePair in v.VariableInfos.Where(x => !x.Value.Inline && x.Value.Index != 0).OrderBy(x => x.Value.Index))
            {
                nodes.InsertRange(insertIndex, keyValuePair.Value.Translated);
                insertIndex += keyValuePair.Value.Translated.Count;
            }

            var evaluationInstructions = (options & TranslationOptions.FoldConstantExpressions) != 0 ? ConstantFolding.Fold(nodes).ToArray() : nodes.ToArray();
            v.MaxStackSize = ComputeMaxStackSizeContext.ComputeMaxStackSize(evaluationInstructions);
            return evaluationInstructions;
        }

        private static void Rec(List<EvaluationInstruction> nodes, List<NamedValue> variables, IAstNode node,
            List<string> formulaParams, Variables variableInfos, TranslationOptions translationOptions)
        {

            switch (node)
            {
                case ExpressionValue v:
                    nodes.Add(new EvaluationInstruction(EvalOp.Const_0, v.F));
                    break;
                case Variable variable:
                    var paramIndex = formulaParams == null ? -1 : formulaParams.IndexOf(variable.Id);
                    if (paramIndex >= 0)
                        nodes.Add(EvaluationInstruction.Param((byte)(paramIndex + 1)));
                    else // not a param, but a user created variable (named value)
                    {
                        if (Constants.TryGetValue(variable.Id, out var constantValue))
                        {
                            nodes.Add(new EvaluationInstruction(EvalOp.Const_0, constantValue));
                            return;
                        }
                        var flag = variable.Id.StartsWith("f", StringComparison.OrdinalIgnoreCase)
                            ? NamedValue.FormulaParamFlag.Float
                            : variable.Id.StartsWith("s", StringComparison.OrdinalIgnoreCase)
                                ? NamedValue.FormulaParamFlag.Formula
                                : NamedValue.FormulaParamFlag.Vector3;
                        var variableParam = new NamedValue(variable.Id, flag);
                        var idx = variables.BinarySearch(variableParam, s_ParamNameComparer);

                        if (idx < 0)
                            variables.Insert(~idx, variableParam);
                        else
                            variableParam = variables[idx];

                        if (!variableInfos.VariableInfos.TryGetValue(variable.Id, out var info))
                        {
                            variableInfos.VariableInfos.Add(variable.Id, info = new VariableInfo());

                            // SUB FORMULA
                            if (variableParam.IsSingleFloat == NamedValue.FormulaParamFlag.Formula)
                            {
                                info.Translated = new List<EvaluationInstruction>();
                                if (info.Translated == null && string.IsNullOrEmpty(variableParam.SubFormulaError))
                                    variableParam.ParseSubFormula();
                                Rec(info.Translated, variables, variableParam.SubFormulaNode, formulaParams, variableInfos, translationOptions);
                                if ((translationOptions & TranslationOptions.FoldConstantExpressions) != 0)
                                    info.Translated = ConstantFolding.Fold(info.Translated);

                                if (info.Translated.Count == 1 && info.Translated[0].Op == EvalOp.Const_0)
                                    info.Inline = true;
                                else
                                    info.Index = variableInfos.NextIndex++;
                            }
                        }


                        /*
                         * = x + x, x = 1+2
                         * = (1+2) + (1+2)
                         * (1 2 + st0) ld0 ld0 + 
                         */


                        if (variableParam.IsSingleFloat == NamedValue.FormulaParamFlag.Formula)
                        {
                            if (info.Inline)
                            {
                                nodes.Add(info.Translated[0]);
                            }
                            else
                            {
                                if (info.Index == 0)
                                    throw new InvalidDataException(
                                        $"The definition of variable '{variable.Id}' is recursive, aborting");
                                nodes.Add(EvaluationInstruction.Ld((byte)info.Index));
                            }
                        }
                        else
                        {
                            var v = variableParam.IsSingleFloat switch
                            {
                                NamedValue.FormulaParamFlag.Float => new float3(variableParam.Value.x),
                                NamedValue.FormulaParamFlag.Vector3 => (float3)variableParam.Value,
                                _ => throw new System.NotImplementedException(),
                            };
                            nodes.Add(new EvaluationInstruction(EvalOp.Const_0, v));
                        }
                    }

                    break;
                case UnOp u:
                    Rec(nodes, variables, u.A, formulaParams, variableInfos, translationOptions);
                    if (u.Type == OpType.Plus)
                        break;
                    if (u.Type == OpType.Minus)
                        nodes.Add(new EvaluationInstruction(EvalOp.Minus_1));
                    else
                        throw new NotImplementedException(u.Type.ToString());
                    break;
                case BinOp bin:
                    // reverse order
                    Rec(nodes, variables, bin.B, formulaParams, variableInfos, translationOptions);
                    Rec(nodes, variables, bin.A, formulaParams, variableInfos, translationOptions);
                    nodes.Add(new EvaluationInstruction(bin.Type switch
                    {
                        OpType.Gt => EvalOp.Gt_2,
                        OpType.Gte => EvalOp.Gte_2,
                        OpType.Lt => EvalOp.Lt_2,
                        OpType.Lte => EvalOp.Lte_2,
                        OpType.Add => EvalOp.Add_2,
                        OpType.Sub => EvalOp.Sub_2,
                        OpType.Mul => EvalOp.Mul_2,
                        OpType.Div => EvalOp.Div_2,
                        OpType.Mod => EvalOp.Mod_2,
                        _ => throw new NotImplementedException(bin.Type.ToString())
                    }));
                    break;
                case FuncCall f:
                    void CheckArgCount(int n)
                    {
                        Assert.AreEqual(f.Arguments.Count, n);
                        // reverse order
                        for (int i = n - 1; i >= 0; i--)
                            Rec(nodes, variables, f.Arguments[i], formulaParams, variableInfos, translationOptions);
                    }

                    if (!Functions.TryGetOverloads(f.Id, out var overloads))
                        throw new InvalidDataException($"Unknown function {f.Id}");
                    var overloadIndex = overloads.FindIndex(o => o.ArgumentCount == f.Arguments.Count);
                    if (overloadIndex == -1)
                        throw new InvalidDataException($"Function {f.Id} expects {String.Join(" or ", overloads.Select(o => o.ArgumentCount).ToString())} arguments, got {f.Arguments.Count}");
                    var overload = overloads[overloadIndex];

                    CheckArgCount(overload.ArgumentCount);
                    nodes.Add(new EvaluationInstruction(overload.OpCode));
                    break;

                default:
                    Debug.LogError("NULL " + node);
                    throw new NotImplementedException(node?.ToString() ?? "null");
            }
        }
    }
}