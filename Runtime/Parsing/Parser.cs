using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BurstExpressions.Runtime.Parsing.AST;

namespace BurstExpressions.Runtime.Parsing
{
    public static class Parser
    {
        internal struct Operator
        {
            public readonly OpType Type;
            public readonly string Str;
            public readonly int Precedence;
            public readonly Associativity Associativity;
            public readonly bool Unary;

            public Operator(OpType type, string str, int precedence, Associativity associativity = Associativity.None,
                bool unary = false)
            {
                Type = type;
                Str = str;
                Precedence = precedence;
                Associativity = associativity;
                Unary = unary;
            }
        }

        internal enum Associativity
        {
            None,
            Left,
            Right,
        }

        internal static readonly Dictionary<OpType, Operator> Ops = new Dictionary<OpType, Operator>
        {
            {OpType.Add, new Operator(OpType.Add, "+", 2, Associativity.Left)},
            {OpType.Sub, new Operator(OpType.Sub, "-", 2, Associativity.Left)},

            {OpType.Mul, new Operator(OpType.Mul, "*", 3, Associativity.Left)},
            {OpType.Div, new Operator(OpType.Div, "/", 3, Associativity.Left)},
            {OpType.Mod, new Operator(OpType.Mod, "%", 3, Associativity.Left)},

            {OpType.LeftParens, new Operator(OpType.LeftParens, "(", 5)},

            // {OpType.Coma, new Operator(OpType.Coma, ",", 1000, Associativity.Left)},

            // {OpType.Plus, new Operator(OpType.Plus, "+", 2000, Associativity.Right, unary: true)},
            {OpType.Minus, new Operator(OpType.Minus, "-", 2000, Associativity.Right, unary: true)},
        };

        static Operator ReadOperator(string input, bool unary)
        {
            foreach (var o in Ops)
            {
                if (o.Value.Str == input && o.Value.Unary == unary)
                    return o.Value;
            }

            throw new InvalidDataException($"Cannot match operator '{(unary ? "u" : "bi")}nary {input}'");
        }

        public static INode Parse(string s, out string error)
        {
            if (s == null)
            {
                error = null;
                return null;
            }
            var output = new Stack<INode>();
            var opStack = new Stack<Operator>();

            Reader r = new Reader(s);

            try
            {
                r.ReadToken();
                error = null;
                return ParseUntil(r, opStack, output, Token.None, 0);
            }
            catch (Exception e)
            {
                error = $"{r.CurrentTokenIndex}: {e.Message}";
                return null;
            }
        }

        public static bool TryPeek<T>(this Stack<T> stack, out T t)
        {
            if (stack.Count != 0)
            {
                t = stack.Peek();
                return true;
            }

            t = default;
            return false;
        }

        private static INode ParseUntil(Reader r, Stack<Operator> opStack, Stack<INode> output, Token readUntilToken,
            int startOpStackSize)
        {
            do
            {
                switch (r.CurrentTokenType)
                {
                    case Token.LeftParens:
                        {
                            opStack.Push(Ops[OpType.LeftParens]);
                            r.ReadToken();
                            INode arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                                opStack.Count);
                            if (r.CurrentTokenType == Token.Coma)
                                throw new InvalidDataException("Tuples not supported");
                            if (r.CurrentTokenType != Token.RightParens)
                                throw new InvalidDataException("Mismatched parens, missing a closing parens");
                            output.Push(arg);

                            while (opStack.TryPeek(out var stackOp) && stackOp.Type != OpType.LeftParens)
                            {
                                opStack.Pop();
                                PopOpOpandsAndPushNode(stackOp);
                            }

                            if (opStack.TryPeek(out var leftParens) && leftParens.Type == OpType.LeftParens)
                                opStack.Pop();
                            else
                                throw new InvalidDataException("Mismatched parens");
                            r.ReadToken();
                            break;
                        }
                    case Token.RightParens:
                        throw new InvalidDataException("Mismatched parens");
                    case Token.Op:
                        {
                            bool unary = r.PrevTokenType == Token.Op ||
                                         r.PrevTokenType == Token.LeftParens ||
                                         r.PrevTokenType == Token.None;
                            var readBinOp = ReadOperator(r.CurrentToken, unary);

                            while (opStack.TryPeek(out var stackOp) &&
                                   // the operator at the top of the operator stack is not a left parenthesis or coma
                                   stackOp.Type != OpType.LeftParens && stackOp.Type != OpType.Coma &&
                                   // there is an operator at the top of the operator stack with greater precedence
                                   (stackOp.Precedence > readBinOp.Precedence ||
                                    // or the operator at the top of the operator stack has equal precedence and the token is left associative
                                    stackOp.Precedence == readBinOp.Precedence &&
                                    readBinOp.Associativity == Associativity.Left))
                            {
                                opStack.Pop();
                                PopOpOpandsAndPushNode(stackOp);
                            }

                            opStack.Push(readBinOp);
                            r.ReadToken();
                            break;
                        }
                    case Token.Number:
                        output.Push(new ExpressionValue(float.Parse(r.CurrentToken, CultureInfo.InvariantCulture)));
                        r.ReadToken();
                        break;
                    case Token.Identifier:
                        var id = r.CurrentToken;
                        r.ReadToken();
                        if (r.CurrentTokenType != Token.LeftParens) // variable
                        {
                            output.Push(new Variable(id));
                            break;
                        }
                        else // function call
                        {
                            r.ReadToken(); // skip (
                            opStack.Push(Ops[OpType.LeftParens]);
                            List<INode> args = new List<INode>();

                            while (true)
                            {
                                INode arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                                    opStack.Count);
                                args.Add(arg);
                                if (r.CurrentTokenType == Token.RightParens)
                                {
                                    opStack.Pop();
                                    break;
                                }
                                r.ReadToken();
                            }

                            r.ReadToken(); // skip )

                            // RecurseThroughArguments(args, arg);
                            output.Push(new FuncCall(id, args));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(r.CurrentTokenType.ToString());
                }
            }
            while (!readUntilToken.HasFlag(r.CurrentTokenType));

            while (opStack.Count > startOpStackSize)
            {
                var readBinOp = opStack.Pop();
                if (readBinOp.Type == OpType.LeftParens)
                    break;
                PopOpOpandsAndPushNode(readBinOp);
            }

            return output.Pop();

            void PopOpOpandsAndPushNode(Operator readBinOp)
            {
                var b = output.Pop();
                if (readBinOp.Unary)
                {
                    output.Push(new UnOp(readBinOp.Type, b));
                }
                else
                {
                    if (output.Count == 0)
                        throw new InvalidDataException($"Missing operand for the {readBinOp.Str} operator in the expression");
                    var a = output.Pop();
                    output.Push(new BinOp(readBinOp.Type, a, b));
                }
            }

            void RecurseThroughArguments(List<INode> args, INode n)
            {
                switch (n)
                {
                    case BinOp b when b.Type == OpType.Coma:
                        RecurseThroughArguments(args, b.A);
                        RecurseThroughArguments(args, b.B);
                        break;
                    default:
                        args.Add(n);
                        break;
                }
            }
        }
    }
}