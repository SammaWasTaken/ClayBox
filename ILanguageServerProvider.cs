using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface ILanguageServerProvider
    {
        public Task<Dictionary<uint, CodeBlock>> ParseText(string text);
    }

    public struct CodeBlock(TextType type, bool error, bool warning)
    {
        public TextType Type = type;

        public bool Error = error;
        public bool Warning = warning;
    }
}
