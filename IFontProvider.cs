using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface IFontProvider
    {
        public ITintableImage ProvideGlyph(char c);
        public uint ProvideGlyphOffset(char c);

        public uint ProvideFontHeight();
    }
}
