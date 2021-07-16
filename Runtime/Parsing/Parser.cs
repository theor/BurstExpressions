using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using BurstExpressions.Runtime.Parsing.AST;

namespace BurstExpressions.Runtime.Parsing
{
    public static class Parser
    {
        public enum ErrorKind
        {
            None,
            ClosingParenMissing,
            TuplesNotSupported,
            MismatchedParens,
            MissingOperand,
            UnknownUnaryOperator,
            UnknownBinaryOperator,
        }

        public readonly struct Error
        {
            public readonly ErrorKind Kind;
            public readonly string Argument;
            public readonly int Location;

            public Error(ErrorKind kind, int location, string argument)
            {
                Kind = kind;
                Argument = argument;
                Location = location;
            }

            public override string ToString()
            {
                string msg = Kind switch
                {
                    ErrorKind.None => "None",
                    ErrorKind.ClosingParenMissing => "Closing paren missing",
                    ErrorKind.TuplesNotSupported => "Tuples not supported",
                    ErrorKind.MismatchedParens => "Mismatched parens",
                    ErrorKind.MissingOperand => $"Missing operand for the {Argument} operator in the expression",
                    ErrorKind.UnknownUnaryOperator => $"Cannot match unary operator '{Argument}'",
                    ErrorKind.UnknownBinaryOperator => $"Cannot match binary operator '{Argument}'",
                    _ => throw new ArgumentOutOfRangeException()
                }
                    ;
                return $"At {Location}: {msg}";
            }
        }

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

        static INode ReturnError(INode node, int location, ref Error error, ErrorKind kind, string argument = null)
        {
            error = new Error(kind, location, argument);
            return node;
        }

        static bool ReadOperator(Reader r, bool unary, out Operator @operator, ref Error error)
        {
            foreach (var o in Ops)
            {
                if (o.Value.Str == r.CurrentToken && o.Value.Unary == unary)
                {
                    @operator = o.Value;
                    return true;
                }
            }

            error = new Error(unary ? ErrorKind.UnknownUnaryOperator : ErrorKind.UnknownBinaryOperator, r.CurrentTokenIndex, r.CurrentToken);
            @operator = default;
            return false;
        }

        public static bool TryParse(string s, out INode node, out Error error)
        {
            node = null;
            if (s == null)
            {
                error = default;
                return true;
            }
            var output = new Stack<INode>();
            var opStack = new Stack<Operator>();

            Reader r = new Reader(s);

            r.ReadToken();
            error = default;
            node = ParseUntil(r, opStack, output, Token.None, 0, out error);
            return error.Kind == ErrorKind.None;

        }
        // public static INode Parse(string s, out string error)
        // {
        //     error = null;
        //     if (TryParse(s, out var node, out var err))
        //     {
        //         return node;
        //     }
        //
        //     error = err.ToString();
        //     return node;
        // }

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
            int startOpStackSize, out Error error)
        {
            error = default;
            do
            {
                switch (r.CurrentTokenType)
                {
                    case Token.LeftParens:
                        {
                            opStack.Push(Ops[OpType.LeftParens]);
                            r.ReadToken();
                            INode arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                                opStack.Count, out error);
                            if (r.CurrentTokenType == Token.Coma)
                                return ReturnError(arg, r.CurrentTokenIndex, ref error, ErrorKind.TuplesNotSupported);
                            if (r.CurrentTokenType != Token.RightParens)
                                return ReturnError(arg, r.CurrentTokenIndex, ref error, ErrorKind.ClosingParenMissing);
                            output.Push(arg);

                            while (opStack.TryPeek(out var stackOp) && stackOp.Type != OpType.LeftParens)
                            {
                                opStack.Pop();
                                if (!PopOpOpandsAndPushNode(stackOp, ref error))
                                    return output.Peek();
                            }

                            if (opStack.TryPeek(out var leftParens) && leftParens.Type == OpType.LeftParens)
                                opStack.Pop();
                            else
                                return ReturnError(arg, r.CurrentTokenIndex, ref error, ErrorKind.MismatchedParens);
                            r.ReadToken();
                            break;
                        }
                    case Token.RightParens:
                        return ReturnError(default, r.CurrentTokenIndex, ref error, ErrorKind.MismatchedParens);
                    case Token.Op:
                        {
                            bool unary = r.PrevTokenType == Token.Op ||
                                         r.PrevTokenType == Token.LeftParens ||
                                         r.PrevTokenType == Token.None;
                            if (!ReadOperator(r, unary, out var readBinOp, ref error))
                                return null;

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
                                if (!PopOpOpandsAndPushNode(stackOp, ref error))
                                    return output.Peek();
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
                                    opStack.Count, out error);
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
                if (!PopOpOpandsAndPushNode(readBinOp, ref error))
                    return output.Peek();
            }

            return output.Pop();

            bool PopOpOpandsAndPushNode(Operator readBinOp, ref Error error)
            {
                var b = output.Pop();
                if (readBinOp.Unary)
                {
                    output.Push(new UnOp(readBinOp.Type, b));
                }
                else
                {
                    if (output.Count == 0)
                    {
                        error = new Error(ErrorKind.MissingOperand, r.CurrentTokenIndex, readBinOp.Str);
                        output.Push(new BinOp(readBinOp.Type, b, null));
                        return false;
                    }
                    var a = output.Pop();
                    output.Push(new BinOp(readBinOp.Type, a, b));
                }

                return true;
            }
        }
    }
}