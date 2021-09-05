using System;
using System.IO;
using System.Text;

namespace BurstExpressions.Runtime.Parsing
{
    public class Reader
    {
        private readonly string _input;
        private int _i;

        public Reader(string input)
        {
            _input = input.Trim();
            _i = 0;
        }

        private void SkipWhitespace()
        {
            while (!Done && Char.IsWhiteSpace(_input[_i]))
                _i++;
        }

        public bool Done => _i >= _input.Length;
        private char NextChar => _input[_i];
        public int CurrentTokenIndex => _i;

        private char ConsumeChar() => _input[_i++];

        public string CurrentToken;
        public Token CurrentTokenType;
        public Token PrevTokenType;

        public void ReadToken()
        {
            CurrentToken = null;
            PrevTokenType = CurrentTokenType;
            CurrentTokenType = Token.None;
            if (Done)
                return;
            if (NextChar == '(')
            {
                ConsumeChar();
                CurrentTokenType = Token.LeftParens;
            }
            else if (NextChar == ')')
            {
                ConsumeChar();
                CurrentTokenType = Token.RightParens;
            }
            else if (NextChar == ',')
            {
                ConsumeChar();
                CurrentTokenType = Token.Coma;
            }
            else if (Char.IsDigit(NextChar) || NextCharIsPoint())
            {
                bool foundPoint = false;
                StringBuilder sb = new StringBuilder();
                do
                {
                    foundPoint |= NextCharIsPoint();
                    sb.Append(ConsumeChar());
                }
                while (!Done && (Char.IsDigit(NextChar) || (NextChar == '.' && !foundPoint)));
                if (!Done && foundPoint && NextCharIsPoint()) // 1.2.3
                    throw new InvalidDataException($"Invalid number: '{sb}.'");

                CurrentToken = sb.ToString();
                CurrentTokenType = Token.Number;
            }
            else
            {
                if (MatchOp(out var op))
                {
                    CurrentToken = op.Str;
                    CurrentTokenType = Token.Op;
                    for (int i = 0; i < CurrentToken.Length; i++)
                        ConsumeChar();
                }
                else
                {
                    CurrentTokenType = Token.Identifier;
                    StringBuilder sb = new StringBuilder();
                    while (!Done && NextChar != ')' && NextChar != ',' && !MatchOp(out _) && !Char.IsWhiteSpace(NextChar))
                        sb.Append(ConsumeChar());
                    CurrentToken = sb.ToString();
                }
            }

            SkipWhitespace();

            bool MatchOp(out Parser.Operator desc)
            {
                foreach (var pair in Parser.Ops)
                {
                    if (_input.IndexOf(pair.Value.Str, _i, StringComparison.Ordinal) != _i)
                        continue;
                    desc = pair.Value;
                    return true;
                }

                desc = default;
                return false;
            }

            bool NextCharIsPoint() => NextChar == '.';
        }
    }
}