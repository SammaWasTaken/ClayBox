using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface IThemeProvider
    {
        public void ProvideBackground(ClayImage canvas);

        public uint ProvideTextColor(TextType type);

        public uint ProvideCursorLineThickness();
        public uint ProvideCursorLineColor();
        public uint ProvideErrorColor();
        public uint ProvideWarningColor();
        public uint ProvideSelectionColor();

        public (uint, uint) ProvideBaseMargin();
    }

    public interface ILineNumberThemeProvider
    {
        public void ProvideBackground(ClayImage canvas);

        public uint ProvideNumberLineThickness();
        public uint ProvideNumberLineColor();
        public Alignment ProvideNumberAlignment();
        public Alignment ProvideNumberLineAlignment();

        public (uint, uint) ProvideBaseMargin();
    }

    public enum Alignment
    {
        Left,
        Right,
    }

    public enum TextType
    {
        Plain,
        Keyword,
        String,
        Number,
        Comment,
        Function,
        Type,
        Variable,
        Constant,
        Preprocessor,
        Operator,
        Punctuation,
        Attribute,
        Class,
        Interface,
        Enum,
        Struct,
        Delegate,
        Event,
        Property,
        Method,
        Field,
        Namespace,
        Label,
        Macro,
        LineNumber,
        Identifier,
    }
}
