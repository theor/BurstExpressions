using System;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Parsing.AST;
using UnityEngine;

namespace BurstExpressions.Runtime
{
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
            var subFormulaNode = SubFormulaNode;
            Parser.TryParse(SubFormula, out subFormulaNode, out var error);
            SubFormulaNode = subFormulaNode;
            SubFormulaError = error.ToString();
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