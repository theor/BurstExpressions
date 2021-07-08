using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eval.Runtime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Eval
{
    /// <summary>
    /// Translates the AST to a flat array containing the reverse polish notation (RPN) of the expression
    /// if the expression is 1 * (2 + 3), the RPN is 1 2 3 + * 
    /// </summary>
    public static class Translator
    {
        private static FormulaParamNameComparer s_ParamNameComparer = new FormulaParamNameComparer();
        public static IComparer<FormulaParam> FormulaParamsCompareByName => s_ParamNameComparer;

        public class VariableInfo
        {
            // Ld_0 op index, base 1
            public int Index;
            public List<EvalGraph.Node> Translated;
            // instead of computing the subformula once and loading the cached result with LD, recompute it everytime
            public bool Inline;
        }

        public class Variables
        {
            public int NextIndex = 1;
            public Dictionary<string, VariableInfo> VariableInfos = new Dictionary<string, VariableInfo>();
        }
        
        internal class FormulaParamNameComparer : IComparer<FormulaParam>
        {
            public int Compare(FormulaParam x, FormulaParam y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        [Flags]
        public enum TranslationOptions
        {
            None = 0,
            FoldConstantExpressions = 1,
        }
        public static EvalGraph.Node[] Translate(INode node, List<FormulaParam> variables, List<string> parameters,
            out Variables v, TranslationOptions options = TranslationOptions.None)
        {
            List<EvalGraph.Node> nodes = new List<EvalGraph.Node>();
            v = new Variables();
            Rec(nodes, variables, node, parameters, v, options);
            int insertIndex = 0;
            foreach (var keyValuePair in v.VariableInfos.Where(x => !x.Value.Inline && x.Value.Index != 0).OrderBy(x => x.Value.Index))
            {
                nodes.InsertRange(insertIndex, keyValuePair.Value.Translated);
                insertIndex += keyValuePair.Value.Translated.Count;
            }
            return (options & TranslationOptions.FoldConstantExpressions) != 0 ? ConstantFolding.Fold(nodes).ToArray() : nodes.ToArray();
        }

        private static void Rec(List<EvalGraph.Node> nodes, List<FormulaParam> variables, INode node,
            List<string> formulaParams, Variables variableInfos, TranslationOptions translationOptions)
        {
            
            switch (node)
            {
                case ExpressionValue v:
                    nodes.Add(new EvalGraph.Node(EvalOp.Const_0, v.F));
                    break;
                case Variable variable:
                    var paramIndex = formulaParams == null ? -1 : formulaParams.IndexOf(variable.Id);
                    if(paramIndex >= 0)
                        nodes.Add(EvalGraph.Node.Param((byte) (paramIndex + 1)));
                    else // not a param, but a user created variable (named value)
                    {
                        var flag = variable.Id.StartsWith("f", StringComparison.OrdinalIgnoreCase)
                            ? FormulaParam.FormulaParamFlag.Float
                            : variable.Id.StartsWith("s", StringComparison.OrdinalIgnoreCase)
                                ? FormulaParam.FormulaParamFlag.Formula
                                : FormulaParam.FormulaParamFlag.Vector3;
                        var variableParam = new FormulaParam(variable.Id, flag);
                        var idx = variables.BinarySearch(variableParam, s_ParamNameComparer);

                        if (idx < 0)
                            variables.Insert(~idx, variableParam);
                        else
                            variableParam = variables[idx];

                        if (!variableInfos.VariableInfos.TryGetValue(variable.Id, out var info)) 
                        {
                            variableInfos.VariableInfos.Add(variable.Id, info = new VariableInfo());
                            
                            // SUB FORMULA
                            if (variableParam.IsSingleFloat == FormulaParam.FormulaParamFlag.Formula)
                            {
                                info.Translated = new List<EvalGraph.Node>();
                                if(info.Translated == null && string.IsNullOrEmpty(variableParam.SubFormulaError))
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


                        if (variableParam.IsSingleFloat == FormulaParam.FormulaParamFlag.Formula)
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
                                nodes.Add(EvalGraph.Node.Ld((byte) info.Index));
                            }
                        }
                        else
                        {
                            var v = variableParam.IsSingleFloat switch
                            {
                                FormulaParam.FormulaParamFlag.Float => new float3(variableParam.Value.x),
                                FormulaParam.FormulaParamFlag.Vector3 => (float3) variableParam.Value,
                                _ => throw new System.NotImplementedException(),
                            };
                            nodes.Add(new EvalGraph.Node(EvalOp.Const_0, v));
                        }
                    }

                    break;
                case UnOp u:
                    Rec(nodes, variables, u.A, formulaParams, variableInfos, translationOptions);
                    if(u.Type == OpType.Plus)
                        break;
                    if(u.Type == OpType.Minus)
                        nodes.Add(new EvalGraph.Node(EvalOp.Minus_1));
                    else
                        throw new NotImplementedException(u.Type.ToString());
                    break;
                case BinOp bin:
                    // reverse order
                    Rec(nodes, variables, bin.B, formulaParams, variableInfos, translationOptions);
                    Rec(nodes, variables, bin.A, formulaParams, variableInfos, translationOptions);
                    nodes.Add(new EvalGraph.Node(bin.Type switch
                    {
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
                
                    if(!Functions.TryGetOverloads(f.Id, out var overloads))
                        throw new InvalidDataException($"Unknown function {f.Id}");
                    var overloadIndex = overloads.FindIndex(o => o.ArgumentCount == f.Arguments.Count);
                    if(overloadIndex == -1)
                        throw new InvalidDataException($"Function {f.Id} expects {String.Join(" or ", overloads.Select(o => o.ArgumentCount).ToString())} arguments, got {f.Arguments.Count}");
                    var overload = overloads[overloadIndex];
                
                    CheckArgCount(overload.ArgumentCount);
                    nodes.Add(new EvalGraph.Node(overload.OpCode));
                    break;

                default:
                    Debug.LogError("NULL");
                    throw new NotImplementedException(node?.ToString() ?? "null");
            }
        }
    }
}