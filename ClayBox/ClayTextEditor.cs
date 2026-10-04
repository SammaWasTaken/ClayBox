using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace ClayBox
{
    public class ClayTextEditor(IFontProvider fontProvider, IThemeProvider themeProvider, uint w, uint h)
    {
        public IFontProvider FontProvider { get; set; } = fontProvider;
        public IThemeProvider ThemeProvider { get; set; } = themeProvider;
        public ILanguageServerProvider LanguageProvider { get; set; } = new BasicLanguageProvider();

        public uint Width { get; private set; } = w;
        public uint Height { get; private set; } = h;
        public uint TabulationSpaces { get; private set; } = 4;

        public bool CursorVisible { get; set; } = true;

        public uint Cursor { get; set; }
        public int CursorSelectionOffset { get; set; }

        public uint ViewScroll => _offsetLine;
        public uint LineCount => (uint)_lines.Count;

        public string SelectedText => _buffer.Substring((int)Cursor + CursorSelectionOffset, Math.Abs(CursorSelectionOffset));

        string _buffer = "";
        public string Text => _buffer;

        ClayImage _image = new(new uint[w * h], w, h);

        Dictionary<uint,CodeBlock> _languageHilight = new();

        Dictionary<(ConsoleKey key, ConsoleModifiers mods), Action<ConsoleKeyInfo>> _keyHandlers = new();

        List<IClayWidget> _widgets = new();

        List<(int start, int count)> _lines = new();
        uint _offsetLine = 0;

        int _desiredColumn = -1;

        Dictionary<char, ITintableImage> GlyphCache = new();
        Dictionary<char, uint> GlyphOffsetCache = new();

        public ClayImage GetImage() => _image;

        public void SetText(string text)
        {
            _buffer = text;

            _lines = new List<(int start, int count)>();
            int start = 0;
            for (int i = 0; i < _buffer.Length; i++)
            {
                if (_buffer[i] == '\n')
                {
                    int count = i - start;
                    if (count > 0 && _buffer[i - 1] == '\r') count--;
                    _lines.Add((start, count));
                    start = i + 1;
                }
            }
            _lines.Add((start, _buffer.Length - start));

            LanguageProvider.ParseText(_buffer).ContinueWith(t => _languageHilight = t.Result);

            CallWidgetEvents(ClayEventType.TextChanged);
        }

        public void AppendAtCursor(string text)
        {
            SetText(_buffer.Insert((int)Cursor, text));
            Cursor += (uint)text.Length;
        }

        int LineOfCursor() => Math.Max(0, _lines.FindLastIndex(l => l.start <= Cursor));

        void MoveVertical(int delta, bool shift)
        {
            if (_lines.Count == 0) return;

            int line = LineOfCursor();
            if (_desiredColumn < 0)
                _desiredColumn = (int)Cursor - _lines[line].start;

            int target = line + delta;

            if (target < 0 || target >= _lines.Count) return;

            var pcursor = Cursor;

            var t = _lines[target];
            Cursor = (uint)(t.start + Math.Min(_desiredColumn, t.count));

            if (shift) CursorSelectionOffset += (int)pcursor - (int)Cursor;
            else CursorSelectionOffset = 0;
        }

        void SelectionDelete()
        {
            int selStart = (int)Cursor + CursorSelectionOffset;
            int selEnd = (int)Cursor;

            if (selStart > selEnd)
            {
                var temp = selStart;
                selStart = selEnd;
                selEnd = temp;
            }

            SetText(_buffer.Remove(selStart, selEnd - selStart));
            Cursor = (uint)selStart;

            CursorSelectionOffset = 0;
        }

        public void RegisterKeyHandler(ConsoleKeyInfo key, Action<ConsoleKeyInfo> handler) =>
            _keyHandlers.Add((key.Key,key.Modifiers), handler);

        public void HandleKey(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.UpArrow && key.Key != ConsoleKey.DownArrow)
                _desiredColumn = -1;

            switch (key.Key)
            {
                case ConsoleKey.Backspace:
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    else if (Cursor > 0)
                    {
                        SetText(_buffer.Remove((int)Cursor - 1, 1));
                        Cursor--;
                    }
                    break;
                case ConsoleKey.Delete:
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    else if (Cursor < _buffer.Length)
                        SetText(_buffer.Remove((int)Cursor, 1));
                    break;
                case ConsoleKey.LeftArrow:
                    if (Cursor > 0)
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            CursorSelectionOffset++;
                        else CursorSelectionOffset = 0;

                        Cursor--;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (Cursor < _buffer.Length)
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            CursorSelectionOffset--;
                        else CursorSelectionOffset = 0;

                        Cursor++;
                    }
                    break;
                case ConsoleKey.UpArrow:
                    MoveVertical(-1, key.Modifiers.HasFlag(ConsoleModifiers.Shift));
                    break;
                case ConsoleKey.DownArrow:
                    MoveVertical(1, key.Modifiers.HasFlag(ConsoleModifiers.Shift));
                    break;
                case ConsoleKey.PageUp:
                    if (_offsetLine > 0) _offsetLine--;
                    CallWidgetEvents(ClayEventType.ScrollChanged);
                    break;
                case ConsoleKey.PageDown:
                    if (_offsetLine < _lines.Count - 1) _offsetLine++;
                    CallWidgetEvents(ClayEventType.ScrollChanged);
                    break;
                case ConsoleKey.Enter:
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    AppendAtCursor("\n");
                    break;
                case ConsoleKey.Tab:
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    AppendAtCursor(new string(' ', (int)TabulationSpaces));
                    break;
                default:
                    if (_keyHandlers.TryGetValue((key.Key,key.Modifiers),out var handle))
                    {
                        handle(key);
                        break;
                    }

                    if (CursorSelectionOffset != 0) SelectionDelete();
                    AppendAtCursor(key.KeyChar.ToString());

                    break;
            }
        }

        public void AddWidget(IClayWidget widget)
        {
            _widgets.Add(widget);
            widget.OnEvent(this, ClayEventType.WidgetAdded);
        }

        void CallWidgetEvents(ClayEventType eventType)
        {
            foreach (var widget in _widgets)
                widget.OnEvent(this, eventType);
        }

        public void Render()
        {
            ThemeProvider.ProvideBackground(_image);

            uint fontHeight = FontProvider.ProvideFontHeight();

            var selStart = (int)Cursor + CursorSelectionOffset;
            var selEnd = (int)Cursor;

            if (selStart > selEnd)
            {
                var temp = selStart;
                selStart = selEnd;
                selEnd = temp;
            }

            CodeBlock block = new();

            var (xBaseOffset, yOffset) = ThemeProvider.ProvideBaseMargin();

            for (int i = (int)_offsetLine; i < _lines.Count; i++)
            {
                if (yOffset >= Height) break;

                var line = _lines[i];
                uint xOffset = xBaseOffset;

                void DrawCursor(uint x)
                {
                    var thickness = ThemeProvider.ProvideCursorLineThickness();
                    _image.FillRectangle(
                        x >= thickness / 2 ? x - thickness / 2 : 0, yOffset + 1,
                        thickness, fontHeight - 2,
                        ThemeProvider.ProvideCursorLineColor()
                    );
                }

                for (int j = line.start; j < line.start + line.count; j++)
                {
                    var character = _buffer[j];
                    CacheGlyph(character);

                    if (_languageHilight.TryGetValue((uint)j, out var cb)) block = cb;
                    if (xOffset >= Width) break;

                    if (CursorSelectionOffset != 0 && j >= selStart && j < selEnd)
                        _image.FillRectangle(
                            xOffset, yOffset,
                            GlyphOffsetCache[character], fontHeight,
                            ThemeProvider.ProvideSelectionColor()
                        );

                    GlyphCache[character].Draw(
                        _image,
                        xOffset, yOffset,
                        ThemeProvider.ProvideTextColor(block.Type)
                    );

                    if (block.Error) _image.DrawSquiggle(
                            yOffset + fontHeight - 1,
                            xOffset, GlyphOffsetCache[character],
                            ThemeProvider.ProvideErrorColor()
                        );
                    else if (block.Warning) _image.DrawSquiggle(
                        yOffset + fontHeight - 1,
                        xOffset, GlyphOffsetCache[character],
                        ThemeProvider.ProvideWarningColor()
                    );

                    if (CursorVisible && Cursor == j) DrawCursor(xOffset);

                    xOffset += GlyphOffsetCache[character];
                }

                if (CursorVisible && Cursor == line.start + line.count && xOffset < Width)
                    DrawCursor(xOffset);

                yOffset += fontHeight;
            }
        }

        void CacheGlyph(char c)
        {
            if (!GlyphCache.ContainsKey(c))
            {
                GlyphCache[c] = FontProvider.ProvideGlyph(c);
                GlyphOffsetCache[c] = FontProvider.ProvideGlyphOffset(c);
            }
        }

        public void Resize(uint w, uint h)
        {
            Width = w;
            Height = h;
            _image.Resize(w,h);

            CallWidgetEvents(ClayEventType.Resized);
        }

        public void InvalidateCahes()
        {
            GlyphCache.Clear();
            GlyphOffsetCache.Clear();
        }
    }
}
