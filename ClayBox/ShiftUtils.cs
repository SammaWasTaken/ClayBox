using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public static class ShiftUtils
    {
        public static CharKind ExtractKind(char c)
        {
            if (char.IsWhiteSpace(c)) return CharKind.Whitespace;
            if (char.IsDigit(c)) return CharKind.Digit;
            if (char.IsSeparator(c)) return CharKind.Separator;
            if (char.IsSymbol(c)) return CharKind.Symbol;
            if (char.IsLetter(c)) return CharKind.Letter;
            return CharKind.Other;
        }

        public static int SearchForEnd(this string str, int start)
        {
            if ((uint)start >= (uint)str.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            var kind = ExtractKind(str[start]);
            int i = start + 1;
            while (i < str.Length && ExtractKind(str[i]) == kind)
                i++;

            return i;
        }

        public static int SearchForStart(this string str, int end)
        {
            if ((uint)end >= (uint)str.Length)
                throw new ArgumentOutOfRangeException(nameof(end));

            var kind = ExtractKind(str[end]);
            int i = end;
            while (i > 0 && ExtractKind(str[i - 1]) == kind)
                i--;

            return i;
        }
    }

    public enum CharKind 
    { 
        Letter, 
        Digit, 
        Whitespace,
        Separator,
        Symbol,
        Other
    }
}
