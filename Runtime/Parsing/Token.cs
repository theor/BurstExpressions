using System;

namespace BurstExpressions.Runtime.Parsing
{
    [Flags]
    public enum Token
    {
        None = 0,
        Op = 1,
        Number = 2,
        Identifier = 4,
        LeftParens = 8,
        RightParens = 16,
        Coma = 32,
    }
}