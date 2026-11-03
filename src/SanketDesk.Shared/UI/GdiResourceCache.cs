#nullable enable
using System;
using System.Collections.Concurrent;
using System.Drawing;

namespace SanketDesk.UI
{
    /// <summary>
    /// Centralized cache and lifetime manager for GDI+ resources (Fonts, Brushes, Pens).
    /// Avoids creating and destroying GDI objects per paint frame or per toast.
    /// Deterministically releases all finite OS GDI handles upon disposal.
    /// </summary>
    internal sealed class GdiResourceCache : IDisposable
    {
        public static readonly GdiResourceCache Instance = new GdiResourceCache();

        private readonly ConcurrentDictionary<int, Font> _fonts;
        private readonly ConcurrentDictionary<int, SolidBrush> _brushes;
        private readonly ConcurrentDictionary<int, Pen> _pens;
        private int _isDisposed;

        public GdiResourceCache()
        {
            _fonts = new ConcurrentDictionary<int, Font>();
            _brushes = new ConcurrentDictionary<int, SolidBrush>();
            _pens = new ConcurrentDictionary<int, Pen>();
            _isDisposed = 0;
        }

        public Font GetFont(string familyName, float sizePt, FontStyle style = FontStyle.Regular)
        {
            int key = (familyName.GetHashCode() * 397) ^ (sizePt.GetHashCode() * 31) ^ (int)style;
            return _fonts.GetOrAdd(key, _ =>
            {
                try
                {
                    return new Font(familyName, sizePt, style);
                }
                catch
                {
                    return new Font(FontFamily.GenericSansSerif, sizePt, style);
                }
            });
        }

        public SolidBrush GetBrush(Color color)
        {
            int key = color.ToArgb();
            return _brushes.GetOrAdd(key, _ => new SolidBrush(color));
        }

        public Pen GetPen(Color color, float width = 1.0f)
        {
            int key = (color.ToArgb() * 397) ^ width.GetHashCode();
            return _pens.GetOrAdd(key, _ => new Pen(color, width));
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            foreach (var font in _fonts.Values)
            {
                try { font.Dispose(); } catch { }
            }
            _fonts.Clear();

            foreach (var brush in _brushes.Values)
            {
                try { brush.Dispose(); } catch { }
            }
            _brushes.Clear();

            foreach (var pen in _pens.Values)
            {
                try { pen.Dispose(); } catch { }
            }
            _pens.Clear();
        }
    }
}
