using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eval.Runtime;

namespace Eval
{
    public static class Functions
    {
        public readonly struct FunctionDefinition
        {
            public readonly int ArgumentCount;
            public readonly EvalOp OpCode;

            public FunctionDefinition(EvalOp opCode, int argumentCount)
            {
                ArgumentCount = argumentCount;
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


        private static void Init()
        {
            var ops = Enum.GetValues(typeof(EvalOp)).Cast<EvalOp>().ToArray();
            _defs = new Dictionary<string, List<FunctionDefinition>>(ops.Length, StringComparer.OrdinalIgnoreCase);
            foreach (EvalOp op in ops)
            {
                if (op == EvalOp.None)
                    continue;
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
                defs.Add(new FunctionDefinition(op, arity));
            }
        }
    }
}