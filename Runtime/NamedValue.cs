using System;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Parsing.AST;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace BurstExpressions.Runtime
{
    [Serializable, MovedFrom(false, sourceClassName:"FormulaParam")]
    public struct NamedValue
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
        public IAstNode SubFormulaNode { get; private set; }

        public static NamedValue FromSubFormula(string name, IAstNode subformula)
        {
            return new NamedValue(name, FormulaParamFlag.Formula) { SubFormulaNode = subformula };
        }

        public void ParseSubFormula()
        {
            var subFormulaNode = SubFormulaNode;
            Parser.TryParse(SubFormula, out subFormulaNode, out var error);
            SubFormulaNode = subFormulaNode;
            SubFormulaError = error.Kind == Parser.ErrorKind.None ? null : error.ToString();
        }

        public NamedValue(string name, FormulaParamFlag isSingleFloat = FormulaParamFlag.Vector3)
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