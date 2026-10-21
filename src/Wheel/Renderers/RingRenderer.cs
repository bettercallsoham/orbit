using System.Windows;
using System.Windows.Media;

namespace Orbit.Wheel.Renderers
{
    /// <summary>A crisp segmented ring with solid selection and a defined central hub.</summary>
    public sealed class RingRenderer : IWheelRenderer
    {
        public string Id => "ring";
        public string DisplayName => "Ring";

        public void Render(DrawingContext dc, WheelFrame f)
        {
            var p = f.Palette;
            double s = f.Scale;
            var center = f.Center;
            double R = f.OuterRadius, r = f.InnerRadius;
            Color accent = f.Accent;
            int count = f.Count;
            double sweep = f.SliceAngle;

            dc.DrawEllipse(p.RingBrush, p.EdgePen, center, R, R);

            if (count > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    double a = f.SliceStart(i);
                    dc.DrawLine(p.DividerPen, f.Polar(r + 6 * s, a), f.Polar(R - 6 * s, a));
                }
            }

            // Hover highlight, faded by the animated hover amount so moving between options glides
            for (int i = 0; i < count; i++)
            {
                double h = f.HoverOf(i);
                if (h < 0.01) continue;
                double start = f.SliceStart(i);
                double rOut = R + 6 * s * h;
                var sector = WheelPainter.AnnularSector(center, r + 2 * s, rOut, start + 0.6, sweep - 1.2);

                var fill = WheelPainter.Solid(ColorUtil.Lerp(p.Ring, Color.FromRgb(0x52, 0x05, 0x7B), h));
                dc.DrawGeometry(fill, WheelPainter.Pen(ColorUtil.WithAlpha(accent, h), 1.5 * s), sector);

                double inset = System.Math.Min(4, sweep * 0.08);
                dc.DrawGeometry(null, WheelPainter.Pen(ColorUtil.WithAlpha(accent, h), 3 * s, round: true),
                    WheelPainter.Arc(center, rOut - 1.5 * s, start + inset, sweep - inset * 2));
            }

            for (int i = 0; i < count; i++)
            {
                double h = f.HoverOf(i);
                double rMid = (r + R) / 2.0 + 3 * s * h;
                var at = f.Polar(rMid, f.SliceMid(i));
                var glyph = WheelPainter.Solid(ColorUtil.Lerp(p.TextMuted, Color.FromRgb(0xBC, 0x6F, 0xF1), h));
                var label = WheelPainter.Solid(ColorUtil.Lerp(p.Text, Colors.White, h));
                using (WheelPainter.Item(dc, f, i, at))
                {
                    WheelPainter.DrawItemContent(dc, f, i, at, WheelPainter.SliceLabelWidth(f, rMid), glyph, label);
                }
            }

            double hubR = r - 7 * s;
            dc.DrawEllipse(p.RingBrush, p.EdgePen, center, r, r);
            dc.DrawEllipse(p.HubBrush, WheelPainter.Pen(ColorUtil.WithAlpha(accent, (byte)0x8C), 1.5), center, hubR, hubR);

            WheelPainter.DrawHubContent(dc, f, hubR, new WheelPainter.HubStyle
            {
                Title = p.TextBrush,
                Body = p.TextMutedBrush,
                Faint = p.TextFaintBrush,
                Accent = accent
            });
        }
    }
}
