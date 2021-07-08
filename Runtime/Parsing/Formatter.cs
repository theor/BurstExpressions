using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Eval
{
    public static class Formatter
    {
        public static class HaltonSequence
        {
            public static double Halton(int index, int nbase)
            {
                double fraction = 1;
                double result = 0;
                while (index > 0)
                {
                    fraction /= nbase;
                    result += fraction * (index % nbase);
                    index = ~~(index / nbase);
                }

                return result;
            }

            // shortcut for later
            public static int HaltonInt(int index, int nbase, int max) => (int) (Halton(index, nbase) * max);
            public static Color ColorFromIndex(int index, int hbase = 3, float s = 1, float v = 0.5f)
            {
                return Color.HSVToRGB((float) HaltonSequence.Halton(index, hbase), s, v);
            }
        }

        [Flags]
        public enum FormatFlags
        {
            None = 0,
            Colors = 1,
            DifferentColorPerNode = 2,
            Indented = 4,
            ParensAroundBinaryOperators = 8,
            NoVariablePrefix = 16,
        }

        public struct Colors
        {
            public Color Variable;
            public Color Value;
            public Color Unary;
            public Color Binary;
            public Color Function;
        }

        public struct Options
        {
            public FormatFlags Flags;
            public Colors Colors;
            public int RandomColorSeed;
            public int Indent;
        }

        private static Colors DefaultColors = new Colors
        {
            Variable = new Color32(193, 145, 255, 255),
            Value = Color.white,
            Unary = new Color32(102, 195, 204, 255),
            Binary = new Color32(102, 195, 204, 255),
            Function = new Color32(56, 200, 140, 255),
        };

        public static Options DefaultOptions = new Options
        {
            Flags = FormatFlags.None,
            Colors = DefaultColors,
            RandomColorSeed = 0,
        };

        static string Break(Options options, string s)
        {
            if ((options.Flags & FormatFlags.Indented) != 0)
            {
                return $"\n{new string(' ', options.Indent * 2)}{s}";
            }

            return s;
        }
        static string BreakIf(bool b, Options options, string s)
        {
            if ((options.Flags & FormatFlags.Indented) != 0)
            {
                return $"{(b ? "\n"  : "")}{new string(' ', options.Indent * 2)}{s}";
            }

            return s;
        }
        
        static string FormatRec(INode n, ref Options options)
        {
            switch (n)
            {
                case ExpressionValue v:
                    return Colorize(v.F.ToString(CultureInfo.InvariantCulture), options.Colors.Value, ref options);
                case Variable v:
                    return Colorize($"{((options.Flags & FormatFlags.NoVariablePrefix) != 0 ? "" : "$")}{v.Id}", options.Colors.Variable, ref options);
                case UnOp un:
                    return Colorize($"{FormatOp(un.Type)}{FormatRec(un.A, ref options)}", options.Colors.Unary, ref options);
                case BinOp binOp:
                    var parens = (options.Flags & FormatFlags.ParensAroundBinaryOperators) != 0;
                    var left = parens ? "(" : "";
                    var right = parens ? Break(options, ")") : "";
                    if(parens) options.Indent++;
                    var a = BreakIf(parens, options, FormatRec(binOp.A, ref options));
                    if(parens) options.Indent--;
                    var op = Break(options, FormatOp(binOp.Type));
                    if(parens) options.Indent++;
                    var b = Break(options, FormatRec(binOp.B, ref options));
                    if(parens) options.Indent--;
                    return Colorize($"{left}{a} {op} {b}{right}", options.Colors.Binary, ref options);
                case FuncCall f:

                    options.Indent++;
                    string joinedArgs = "";
                    for (var i = 0; i < f.Arguments.Count; i++)
                    {
                        if (i > 0)
                            joinedArgs += ", ";
                        joinedArgs += Break(options, FormatRec(f.Arguments[i], ref options));
                    }
                    options.Indent--;
                    return Colorize($"{f.Id}({joinedArgs}{Break(options, ")")}", options.Colors.Function, ref options);
                default:
                    throw new NotImplementedException(n.ToString());
            }
        }

        private static string Colorize(string s, Color c, ref Options options) =>
            options.Flags.HasFlag(FormatFlags.Colors)
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>"
                : (options.Flags & FormatFlags.DifferentColorPerNode) != 0
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(HaltonSequence.ColorFromIndex(options.RandomColorSeed++, s:0.45f, v: 1))}>{s}</color>"
                    : s;

        public static string Format(INode n, FormatFlags flags = FormatFlags.None)
        {
            Options options = DefaultOptions;
            options.Flags = flags;
            return FormatRec(n, ref options);
        }
        public static string Format(INode n, Options options)
        {
            Options copy = options;
            return FormatRec(n, ref copy);
        }

        private static string FormatOp(OpType bType)
        {
            return Parser.Ops[bType].Str;
        }
    }
}