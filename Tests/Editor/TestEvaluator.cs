using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurstExpressions.Runtime.Parsing;
using BurstExpressions.Runtime.Parsing.AST;
using NUnit.Framework;
using Unity.Mathematics;

namespace Tests.Editor
{
    public static class TestEvaluator
    {
        public static float Eval(INode node, Dictionary<string, float> variables = null)
        {
            switch (node)
            {
                case ExpressionValue v:
                    return v.F;
                case Variable variable:
                    return variables[variable.Id];
                case UnOp u:
                    return u.Type == OpType.Plus ? Eval(u.A, variables) : -Eval(u.A, variables);
                case BinOp bin:
                    var a = Eval(bin.A, variables);
                    var b = Eval(bin.B, variables);
                    switch (bin.Type)
                    {
                        case OpType.Add:
                            return a + b;
                        case OpType.Sub:
                            return a - b;
                        case OpType.Mul:
                            return a * b;
                        case OpType.Div:
                            return a / b;
                        case OpType.Mod:
                            return a % b;
                        default:
                            throw new ArgumentOutOfRangeException(bin.Type.ToString());
                    }
                case FuncCall f:
                    void CheckArgCount(int n) => Assert.AreEqual(f.Arguments.Count, n);
                    switch (f.Id)
                    {
                        case "tan": return math.tan(Eval(f.Arguments.Single(), variables));
                        case "sin": return math.sin(Eval(f.Arguments.Single(), variables));
                        case "cos": return math.sin(Eval(f.Arguments.Single(), variables));
                        case "sqrt": return math.sqrt(Eval(f.Arguments.Single(), variables));
                        case "abs": return math.abs(Eval(f.Arguments.Single(), variables));
                        case "pow":
                            CheckArgCount(2);
                            return math.pow(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                        case "min":
                            CheckArgCount(2);
                            return math.min(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                        case "max":
                            CheckArgCount(2);
                            return math.max(Eval(f.Arguments[0], variables), Eval(f.Arguments[1], variables));
                        default: throw new InvalidDataException($"Unknown function {f.Id}");
                    }

                default: throw new NotImplementedException();
            }
        }
    }
}