using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BurstExpressions.Runtime.Runtime;

namespace BurstExpressions.Runtime.Parsing
{
    public static class Functions
    {
        public readonly struct FunctionDefinition
        {
            public readonly string Signature;
            public readonly int ArgumentCount;
            public readonly string HelpText;
            public readonly EvalOp OpCode;

            public FunctionDefinition(EvalOp opCode, int argumentCount, string helpText, string attrSignature)
            {
                Signature = attrSignature;
                ArgumentCount = argumentCount;
                HelpText = helpText;
                OpCode = opCode;
            }
        }

        private static Dictionary<string, List<FunctionDefinition>> _defs;
        public static bool TryGetOverloads(string functionId, out List<FunctionDefinition> overloads)
        {
            if (_defs == null)
                Init();
            return _defs.TryGetValue(functionId, out overloads);
        }

        public static ILookup<string, FunctionDefinition> AllFunctions
        {
            get
            {
                Init();
                return _defs.SelectMany(x => x.Value.Select(d => (x.Key, d)))
                    .ToLookup(x => x.Key, x => x.d);
            }
        }


        private static void Init()
        {
            var ops = Enum.GetValues(typeof(EvalOp)).Cast<EvalOp>().ToArray();
            _defs = new Dictionary<string, List<FunctionDefinition>>(ops.Length, StringComparer.OrdinalIgnoreCase);
            foreach (EvalOp op in ops)
            {
                if (op == EvalOp.None)
                    continue;
                var attr = GetAttributeOfType<OpDescriptionAttribute>(op);
                var str = op.ToString();
                int underscoreIndex = str.LastIndexOf('_');
                if (underscoreIndex < 0)
                    throw new InvalidDataException($"Operator {op} must be suffixed with an underscore and the number of expected parameters. If it takes 2 arguments, it must be named {op}_2.");
                var opName = str.Substring(0, underscoreIndex);
                string arityString = str.Substring(underscoreIndex + 1);
                if (!int.TryParse(arityString, out int arity))
                    throw new InvalidDataException($"Operator {op}'s argument count is not a valid int: '{arityString}'.");
                if (!_defs.TryGetValue(opName, out var defs))
                    _defs.Add(opName, defs = new List<FunctionDefinition>());
                defs.Add(new FunctionDefinition(op, arity, attr?.Description, attr?.Signature));
            }
        }

        internal static T GetAttributeOfType<T>(this Enum enumVal) where T : System.Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }
    }
}