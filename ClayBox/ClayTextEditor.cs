using System.Diagnostics;
using System.Numerics;
using System.Reflection.PortableExecutable;

namespace ClayBox
{
    public class ClayTextEditor
    {
        public IFontProvider FontProvider { get; set; }
        public IThemeProvider ThemeProvider { get; set; }
        public ILanguageServerProvider LanguageProvider { get; set; } = new BasicLanguageProvider();

        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint TabulationSpaces { get; private set; } = 4;

        public bool CursorVisible { get; set; } = true;

        public uint Cursor
        {
            get => _cursor; set
            {
                _desiredColumn = -1;

                _cursor = value;
                var target = LineOfCursor();

                var (mx, my) = ThemeProvider.ProvideBaseMargin();

                var maxline = Math.Floor(
                    (float)(Height - my) / 
                    FontProvider.ProvideFontHeight()
                ) + _offsetLine - 1;

                if (target < _offsetLine) ViewScrollY = (uint)target;
                if (target > maxline) ViewScrollY += (uint)(target - maxline);

                int cx = (int)_charX[Math.Min((int)_cursor, _buffer.Length)];
                int visibleW = (int)Width - (int)mx;
                const int pad = 8;

                if (cx < _offsetX + pad)
                    ViewScrollX = (uint)Math.Max(0, cx - pad);
                else if (cx > _offsetX + visibleW - pad)
                    ViewScrollX = (uint)(cx - visibleW + pad);
            }
        }
        public int CursorSelectionOffset { get; set; }

        public uint ViewScrollY
        {
            get => _offsetLine; set
            {
                _offsetLine = Math.Min(value, LineCount - 1);
                CallWidgetEvents(ClayEventType.VerticalScrollChanged);
            }
        }
        public uint ViewScrollX
        {
            get => _offsetX;
            set
            {
                _offsetX = Math.Min(value, _maxLinePx);
                CallWidgetEvents(ClayEventType.HorizontalScrollChanged);
            }
        }

        public uint LineCount => (uint)_lines.Count;

        public string SelectedText => _buffer.Substring((int)Cursor + CursorSelectionOffset, Math.Abs(CursorSelectionOffset));

        string _buffer = "";
        public string Text => _buffer;

        ClayImage _image;

        Dictionary<uint,CodeBlock> _languageHilight = new();

        readonly Dictionary<ConsoleKey, List<KeyHandler>> _keyHandlers = new();

        List<IClayWidget> _widgets = new();

        List<(int start, int count, uint linepx)> _lines = new();
        uint _offsetLine = 0;
        uint _offsetX = 0;

        uint[] _charX = new uint[1];
        uint _maxLinePx = 0;

        uint _cursor = 0;

        int _desiredColumn = -1;

        Dictionary<char, ITintableImage> GlyphCache = new();
        Dictionary<char, uint> GlyphOffsetCache = new();

        public ClayImage GetImage() => _image;

        public ClayTextEditor(IFontProvider fontProvider, IThemeProvider themeProvider, uint w, uint h, bool defaultEvents = true)
        {
            FontProvider = fontProvider;
            ThemeProvider = themeProvider;

            Width = w;
            Height = h;

            _image = new(new uint[w * h], w, h);

            if (defaultEvents)
            {
                RegisterKeyHandler(ConsoleKey.Backspace, ConsoleModifiers.None, k =>
                {
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    else if (Cursor > 0)
                    {
                        SetText(_buffer.Remove((int)Cursor - 1, 1));
                        Cursor--;
                    }
                }, false);

                RegisterKeyHandler(ConsoleKey.Delete, ConsoleModifiers.None, k =>
                {
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    else if (Cursor < _buffer.Length)
                        SetText(_buffer.Remove((int)Cursor, 1));
                }, false);

                RegisterKeyHandler(ConsoleKey.LeftArrow, ConsoleModifiers.None, k =>
                {
                    if (Cursor > 0)
                    {
                        if (k.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            CursorSelectionOffset++;
                        else CursorSelectionOffset = 0;

                        Cursor--;
                    }
                }, false);

                RegisterKeyHandler(ConsoleKey.RightArrow, ConsoleModifiers.None, k =>
                {
                    if (Cursor < _buffer.Length)
                    {
                        if (k.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            CursorSelectionOffset--;
                        else CursorSelectionOffset = 0;

                        Cursor++;
                    }
                }, false);

                RegisterKeyHandler(ConsoleKey.UpArrow, ConsoleModifiers.None, k =>
                    MoveVertical(-1, k.Modifiers.HasFlag(ConsoleModifiers.Shift)),
                    false
                );

                RegisterKeyHandler(ConsoleKey.DownArrow, ConsoleModifiers.None, k =>
                    MoveVertical(1, k.Modifiers.HasFlag(ConsoleModifiers.Shift)),
                    false
                );

                RegisterKeyHandler(ConsoleKey.PageUp, ConsoleModifiers.None, k =>
                { if (_offsetLine > 0) ViewScrollY--; },
                    false
                );

                RegisterKeyHandler(ConsoleKey.PageDown, ConsoleModifiers.None, k =>
                { if (_offsetLine < _lines.Count - 1) ViewScrollY++; },
                    false
                );

                RegisterKeyHandler(ConsoleKey.Enter, ConsoleModifiers.None, k =>
                {
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    AppendAtCursor("\n");
                }, false);

                RegisterKeyHandler(ConsoleKey.Tab, ConsoleModifiers.None, k =>
                {
                    if (CursorSelectionOffset != 0) SelectionDelete();
                    AppendAtCursor(new string(' ', (int)TabulationSpaces));
                }, false);
            }
        }

        public void SetText(string text)
        {
            _buffer = text;
            _lines = new List<(int start, int count, uint linepx)>();
            _charX = new uint[_buffer.Length + 1];
            _maxLinePx = 0;

            int start = 0;
            uint linespx = 0;

            for (int i = 0; i < _buffer.Length; i++)
            {
                _charX[i] = linespx;

                if (_buffer[i] == '\n')
                {
                    int count = i - start;
                    if (count > 0 && _buffer[i - 1] == '\r') count--;
                    _lines.Add((start, count, linespx));
                    _maxLinePx = Math.Max(_maxLinePx, linespx);

                    linespx = 0;
                    start = i + 1;
                }
                else if (_buffer[i] != '\r')
                {
                    CacheGlyph(_buffer[i]);
                    linespx += GlyphOffsetCache[_buffer[i]];
                }
            }

            _charX[_buffer.Length] = linespx;
            _lines.Add((start, _buffer.Length - start, linespx));
            _maxLinePx = Math.Max(_maxLinePx, linespx);

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

            var dcback = _desiredColumn;

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

            _desiredColumn = dcback;
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

        public void RegisterKeyHandler(
            ConsoleKey key,
            ConsoleModifiers mods,
            Action<ConsoleKeyInfo> action,
            bool exact = true
        ){
            if (!_keyHandlers.TryGetValue(key, out var list))
                _keyHandlers[key] = list = new();

            list.RemoveAll(h => h.Mods == mods && h.Exact == exact);
            list.Add(new KeyHandler(mods, exact, action));
        }

        KeyHandler? FindHandler(ConsoleKeyInfo key)
        {
            if (!_keyHandlers.TryGetValue(key.Key, out var list)) return null;

            KeyHandler? best = null;
            int bestScore = -1;

            foreach (var h in list)
            {
                bool match = h.Exact
                    ? key.Modifiers == h.Mods
                    : (key.Modifiers & h.Mods) == h.Mods;

                if (!match) continue;

                int score = h.Exact ? 100 : BitOperations.PopCount((uint)h.Mods);

                if (score > bestScore)
                {
                    best = h;
                    bestScore = score;
                }
            }

            return best;
        }

        public void HandleKey(ConsoleKeyInfo key)
        {
            var handler = FindHandler(key);
            if (handler != null)
            {
                handler.Action(key);
                return;
            }

            if (CursorSelectionOffset != 0) SelectionDelete();
            AppendAtCursor(key.KeyChar.ToString());
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
                int xOffset = (int)xBaseOffset - (int)_offsetX;

                void DrawCursor(int x)
                {
                    var thickness = (int)ThemeProvider.ProvideCursorLineThickness();
                    _image.FillRectangle(
                        x - thickness / 2, (int)yOffset + 1,
                        (uint)thickness, fontHeight - 2,
                        ThemeProvider.ProvideCursorLineColor()
                    );
                }

                for (int j = line.start; j < line.start + line.count; j++)
                {
                    var character = _buffer[j];

                    if (_languageHilight.TryGetValue((uint)j, out var cb)) block = cb;
                    if (xOffset >= Width) break;

                    if (CursorSelectionOffset != 0 && j >= selStart && j < selEnd)
                        _image.FillRectangle(
                            xOffset, (int)yOffset,
                            GlyphOffsetCache[character], fontHeight,
                            ThemeProvider.ProvideSelectionColor()
                        );

                    GlyphCache[character].Draw(
                        _image,
                        xOffset, (int)yOffset,
                        ThemeProvider.ProvideTextColor(block.Type)
                    );

                    if (block.Error) _image.DrawSquiggle(
                        xOffset, (int)(yOffset + fontHeight - 1),
                        (int)GlyphOffsetCache[character],
                        ThemeProvider.ProvideErrorColor()
                    );
                    else if (block.Warning) _image.DrawSquiggle(
                        xOffset, (int)(yOffset + fontHeight - 1),
                        (int)GlyphOffsetCache[character],
                        ThemeProvider.ProvideWarningColor()
                    );

                    if (CursorVisible && Cursor == j) DrawCursor(xOffset);

                    xOffset += (int)GlyphOffsetCache[character];
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

        public void RebuildCahes()
        {
            GlyphCache.Clear();
            GlyphOffsetCache.Clear();

            SetText(_buffer);
        }
    }

    record KeyHandler(ConsoleModifiers Mods, bool Exact, Action<ConsoleKeyInfo> Action);
}
