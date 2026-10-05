using System;
using System.Threading.Tasks;

namespace ClayBox
{
    public sealed class ClayBitMask : ITintableImage
    {
        private readonly byte[] _data;

        public int Width { get; }
        public int Height { get; }

        public ReadOnlySpan<byte> RawData => _data;

        public ClayBitMask(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _data = new byte[((long)width * height + 1) / 2 > int.MaxValue
                ? throw new ArgumentException("Image too large")
                : (int)(((long)width * height + 1) / 2)];
        }

        public ClayBitMask(int width, int height, ReadOnlySpan<byte> packed) : this(width, height)
        {
            if (packed.Length != _data.Length)
                throw new ArgumentException($"Expected {_data.Length} bytes, got {packed.Length}.");
            packed.CopyTo(_data);
        }

        public Channel GetPixel(int x, int y)
        {
            int i = Index(x, y);
            int shift = (i & 1) << 2;
            return (Channel)((_data[i >> 1] >> shift) & 0x0F);
        }

        public void SetPixel(int x, int y, Channel value)
        {
            int i = Index(x, y);
            int shift = (i & 1) << 2;
            int b = _data[i >> 1];
            b &= ~(0x0F << shift);
            b |= ((int)value & 0x0F) << shift;
            _data[i >> 1] = (byte)b;
        }

        public bool GetChannel(int x, int y, Channel channel)
            => (GetPixel(x, y) & channel) != 0;

        public void SetChannel(int x, int y, Channel channel, bool on)
        {
            Channel p = GetPixel(x, y);
            SetPixel(x, y, on ? (p | channel) : (p & ~channel));
        }


        public void Clear() => Array.Clear(_data, 0, _data.Length);

        public void Fill(Channel value)
        {
            byte v = (byte)((int)value & 0x0F);
            Array.Fill(_data, (byte)(v | (v << 4)));
        }

        private int Index(int x, int y)
        {
            if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
            if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
            return y * Width + x;
        }

        public void Draw(ClayImage canvas, int x, int y, uint color)
        {
            if (x + Width < 0 || y + Height < 0) return;
            if (x > canvas.Width || y > canvas.Height) return;

            for (int my = 0; my < Height; my++)
            {
                long py = y + my;
                if (py >= canvas.Height) break;

                for (int mx = 0; mx < Width; mx++)
                {
                    long px = x + mx;
                    if (px >= canvas.Width) break;

                    Channel p = GetPixel(mx, my);
                    if (p == Channel.None) continue;

                    uint m = ((p & Channel.A) != 0 ? 0xFF000000u : 0)
                           | ((p & Channel.R) != 0 ? 0x00FF0000u : 0)
                           | ((p & Channel.G) != 0 ? 0x0000FF00u : 0)
                           | ((p & Channel.B) != 0 ? 0x000000FFu : 0);

                    ref uint dst = ref canvas.Data[py * canvas.Width + px];
                    dst = (dst & ~m) | (color & m);
                }
            }
        }
    }

    [Flags]
    public enum Channel : byte
    {
        None = 0,
        R = 1,
        G = 2,
        B = 4,
        A = 8,
        All = 15
    }
}