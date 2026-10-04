using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public class ClayImage(uint[] data, uint w, uint h) : ITintableImage
    {
        public uint Width { get; private set; } = w;
        public uint Height { get; private set; } = h;

        public uint[] Data { get; set; } = data;

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

        static uint ColorScale(uint a, uint b) => (a * b + 127) / 255;

        static uint ColorMix(uint bg, uint fg, uint cov) => (bg * (255 - cov) + fg * cov + 127) / 255;

        public void Draw(ClayImage canvas, uint x, uint y, uint color)
        {
            if (x >= canvas.Width || y >= canvas.Height) return;

            uint drawW = Math.Min(Width, canvas.Width - x);
            uint drawH = Math.Min(Height, canvas.Height - y);

            uint tintA = color >> 24;
            if (tintA == 0) return;

            uint fr = (color >> 16) & 0xFF;
            uint fg = (color >> 8) & 0xFF;
            uint fb = color & 0xFF;

            for (uint sy = 0; sy < drawH; sy++)
            {
                int srcRow = (int)(sy * Width);
                int dstRow = (int)((y + sy) * canvas.Width + x);

                for (uint sx = 0; sx < drawW; sx++)
                {
                    uint src = Data[srcRow + sx];
                    if (src == 0) continue;

                    uint cr = ColorScale((src >> 16) & 0xFF, tintA);
                    uint cg = ColorScale((src >> 8) & 0xFF, tintA);
                    uint cb = ColorScale(src & 0xFF, tintA);
                    uint ca = ColorScale(src >> 24, tintA);

                    if ((cr | cg | cb | ca) == 0) continue;

                    int di = dstRow + (int)sx;
                    uint dst = canvas.Data[di];
                    uint da = dst >> 24;

                    if (da == 255)
                    {
                        uint r = ColorMix((dst >> 16) & 0xFF, fr, cr);
                        uint g = ColorMix((dst >> 8) & 0xFF, fg, cg);
                        uint b = ColorMix(dst & 0xFF, fb, cb);
                        canvas.Data[di] = 0xFF000000 | (r << 16) | (g << 8) | b;
                    }
                    else
                    {
                        uint outA = ca + ColorScale(da, 255 - ca);
                        if (outA == 0) continue;

                        uint r = (fr * ca + ColorScale((dst >> 16) & 0xFF, 255 - ca) * da / 255) / outA;
                        uint g = (fg * ca + ColorScale((dst >> 8) & 0xFF, 255 - ca) * da / 255) / outA;
                        uint b = (fb * ca + ColorScale(dst & 0xFF, 255 - ca) * da / 255) / outA;

                        canvas.Data[di] = (outA << 24)
                                 | (Math.Min(r, 255u) << 16)
                                 | (Math.Min(g, 255u) << 8)
                                 | Math.Min(b, 255u);
                    }
                }
            }
        }
    }
}
