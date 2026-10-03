using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public class ClayImage(uint[] data, uint w, uint h)
    {
        public uint Width { get; private set; } = w;
        public uint Height { get; private set; } = h;

        public uint[] Data { get; set; } = data;

        public void DrawBitMask(ClayBitMask mask, uint x, uint y, uint color)
        {
            for (int my = 0; my < mask.Height; my++)
            {
                long py = y + my;
                if (py >= Height) break;

                for (int mx = 0; mx < mask.Width; mx++)
                {
                    long px = x + mx;
                    if (px >= Width) break;

                    Channel p = mask.GetPixel(mx, my);
                    if (p == Channel.None) continue;

                    uint m = ((p & Channel.A) != 0 ? 0xFF000000u : 0)
                           | ((p & Channel.R) != 0 ? 0x00FF0000u : 0)
                           | ((p & Channel.G) != 0 ? 0x0000FF00u : 0)
                           | ((p & Channel.B) != 0 ? 0x000000FFu : 0);

                    ref uint dst = ref Data[py * Width + px];
                    dst = (dst & ~m) | (color & m);
                }
            }
        }

        public void DrawVertialLine(uint x, uint y1, uint y2, uint color)
        {
            if (x >= Width) return;

            if (y1 > y2) (y1, y2) = (y2, y1);
            if (y2 >= Height) y2 = Height - 1;

            for (uint y = y1; y <= y2; y++)
            {
                Data[y * Width + x] = color;
            }
        }

        public void DrawHorizontalLine(uint y, uint x1, uint x2, uint color)
        {
            if (y >= Height) return;

            if (x1 > x2) (x1, x2) = (x2, x1);
            if (x2 >= Width) x2 = Width - 1;

            Array.Fill(Data, color, (int)(y * Width + x1), (int)(x2 - x1));
        }

        public void DrawSquiggle(uint y, uint x1, uint x2, uint color)
        {
            for (int i = (int)x1; i < x1 + x2; i++)
            {
                double dat = y + Math.Sin(i);

                Data[(int)((int)dat * Width + i)] = color;
            }
        }

        public void FillRectangle(uint x, uint y, uint w, uint h, uint color)
        {
            if (x >= Width || y >= Height) return;

            if (h == 1)
            {
                DrawHorizontalLine(y, x, x + w, color);
                return;
            }
            else if (w == 1)
            {
                DrawVertialLine(x, y, y + h, color);
                return;
            }

            if (x + w > Width) w = Width - x;
            if (y + h > Height) h = Height - y;

            for (uint dy = 0; dy < h; dy++)
                Array.Fill(Data, color, (int)((y + dy) * Width + x), (int)w);
        }

        public void Resize(uint w, uint h)
        {
            if (w == Width && h == Height) return;
            Width = w;
            Height = h;
            Data = new uint[w * h];
        }
    }
}
