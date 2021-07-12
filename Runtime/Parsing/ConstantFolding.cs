using System.Collections.Generic;
using System.Linq;
using BurstExpressions.Runtime.Runtime;
using Unity.Mathematics;

namespace BurstExpressions.Runtime.Parsing
{
    public static class ConstantFolding
    {
        public struct FoldContext : IContext
        {
            public static FoldContext New() => new FoldContext
            {
                Stack = new List<Val>(),
            };

            struct Val
            {
                public float3 Value;
                public bool Foldable;
            }

            private List<Val> Stack;
            private int _popped, _pushed;
            private bool _foldable;

            public void StartNode(Node n)
            {
                _popped = _pushed = 0;
                _foldable = true;
            }

            public void EndNode(List<Node> nodes, Node node)
            {
                if (!_foldable)
                    nodes.Add(node);
                else
                {
                    for (int i = 0; i < _popped; i++)
                    {
                        nodes.RemoveAt(nodes.Count - 1);
                    }
                    for (int i = 0; i < _pushed; i++)
                    {
                        nodes.Add(new Node(EvalOp.Const_0, Stack[Stack.Count - _pushed + i].Value));
                    }
                }
            }
            public float3 Param(byte paramIndex)
            {
                _foldable = false;
                return float3.zero;
            }

            public float3 Load(byte paramIndex)
            {
                _foldable = false;
                return float3.zero;
            }

            public float3 Pop()
            {
                _popped++;
                var x = Stack[Stack.Count - 1];
                if (!x.Foldable)
                    _foldable = false;
                Stack.RemoveAt(Stack.Count - 1);
                return x.Value;
            }

            public void Push(float3 val)
            {
                Stack.Add(new Val { Value = val, Foldable = _foldable });
                _pushed++;
            }
        }
        public static List<Node> Fold(IEnumerable<Node> nodes)
        {
            var current = 0;
            var defaultOps = default(Evaluator.DefaultOps);
            FoldContext ctx = FoldContext.New();
            List<Node> result = new List<Node>();
            var count = nodes.Count();
            while (current < count)
            {
                var node = nodes.ElementAt(current);
                ctx.StartNode(node);
                defaultOps.ExecuteOp(node, ref ctx);
                ctx.EndNode(result, node);
                current++;
            }

            return result;
            // switch (node)
            // {
            //     case ExpressionValue expressionValue:
            //         break;
            //     case FuncCall funcCall:
            //         break;
            //     case BinOp binOp:
            //         break;
            //     case UnOp unOp:
            //         var opand = Fold(unOp.A);
            //         if(opand is ex)
            //         break;
            //     case Variable variable:
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException(nameof(node));
            // }
            throw new System.NotImplementedException();
        }
    }
}