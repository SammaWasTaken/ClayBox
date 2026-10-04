using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface ITintableImage
    {
        public void Draw(ClayImage canvas, uint x, uint y, uint color);
    }
}
