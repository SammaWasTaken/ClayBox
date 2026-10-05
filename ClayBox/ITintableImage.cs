using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface ITintableImage
    {
        public void Draw(ClayImage canvas, int x, int y, uint color);
    }
}
