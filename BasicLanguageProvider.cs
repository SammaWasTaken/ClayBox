using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public class BasicLanguageProvider : ILanguageServerProvider
    {
        public async Task<Dictionary<uint, CodeBlock>> ParseText(string text) => new() {};
    }
}
