using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public class ClayLineNumberWidget(ILineNumberThemeProvider themeProvider) : IClayWidget
    {
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public ILineNumberThemeProvider ThemeProvider { get; set; } = themeProvider;

        public uint LineStartOffset { get; set; } = 1;

        Dictionary<char, ITintableImage> GlyphCache = new();
        Dictionary<char, uint> GlyphOffsetCache = new();

        ClayImage _image = new([], 0, 0);

        public ClayImage GetImage() => _image;

        public void OnEvent(ClayTextEditor textbox, ClayEventType eventType)
        {
            var alignment = ThemeProvider.ProvideNumberAlignment();
            var lineAlignment = ThemeProvider.ProvideNumberLineAlignment();

            uint fontHeight = textbox.FontProvider.ProvideFontHeight();

            var (xOffset, yOffset) = ThemeProvider.ProvideBaseMargin();

            var lw = ThemeProvider.ProvideNumberLineThickness();

            var lineoffLeft = lineAlignment == Alignment.Left ? lw : 0;
            var lineoffRight = lineAlignment == Alignment.Right ? lw : 0;

            string maxLine = (textbox.LineCount - 1 + LineStartOffset).ToString();

            var lineAlloc = xOffset * 2;

            foreach (var item in maxLine)
            {
                CacheGlyph(textbox.FontProvider, item);
                lineAlloc += GlyphOffsetCache[item];
            }

            var width = lineAlloc + lw;

            if (textbox.Height != Height || Width != width)
            {
                Width = width;
                Height = textbox.Height;
                _image.Resize(Width, Height);
            }

            textbox.ThemeProvider.ProvideBackground(_image);

            for (int i = (int)textbox.ViewScrollY; i < textbox.LineCount; i++)
            {
                uint xLineOffset = alignment == Alignment.Right ? width - lineoffRight - xOffset * 2 : xOffset + lineoffLeft;

                var lstr = (i + LineStartOffset).ToString().ToCharArray();

                if (alignment == Alignment.Right)
                    lstr = lstr.Reverse().ToArray();

                foreach (var item in lstr)
                {
                    CacheGlyph(textbox.FontProvider,item);

                    GlyphCache[item].Draw(
                        _image,
                        (int)xLineOffset, (int)yOffset,
                        textbox.ThemeProvider.ProvideTextColor(TextType.LineNumber)
                    );

                    xLineOffset += (uint)(alignment == Alignment.Right ? -1 : 1) * GlyphOffsetCache[item];
                }

                yOffset += fontHeight;
            }

            _image.FillRectangle(
                lineAlignment == Alignment.Right ? (int)lineAlloc : 0, 0, lw, Height,
                ThemeProvider.ProvideNumberLineColor()
            );
        }

        void CacheGlyph(IFontProvider fontProvider, char c)
        {
            if (!GlyphCache.ContainsKey(c))
            {
                GlyphCache[c] = fontProvider.ProvideGlyph(c);
                GlyphOffsetCache[c] = fontProvider.ProvideGlyphOffset(c);
            }
        }
    }
}
