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

        public uint ProvideNumberLineThickness();
        public uint ProvideNumberLineColor();
        public uint ProvideCursorLineThickness();
        public uint ProvideCursorLineColor();
        public uint ProvideErrorColor();
        public uint ProvideWarningColor();
        public uint ProvideSelectionColor();
    }

    public enum TextType
    {
        Standard,
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
    }
}
