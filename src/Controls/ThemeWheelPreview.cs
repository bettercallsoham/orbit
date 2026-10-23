using System;
using System.Windows;
using System.Windows.Media;
using Orbit.Wheel;

namespace Orbit.Controls
{
    /// <summary>A static, hook-free miniature of the built-in ring wheel for a Settings card.</summary>
    public sealed class ThemeWheelPreview : FrameworkElement
    {
        public static readonly DependencyProperty ThemeNameProperty = DependencyProperty.Register(
            nameof(ThemeName), typeof(string), typeof(ThemeWheelPreview),
            new FrameworkPropertyMetadata("Eclipse", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ThemeName
        {
            get => (string)GetValue(ThemeNameProperty);
            set => SetValue(ThemeNameProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var p = WheelPalette.PreviewFor(ThemeName);
            var bounds = new Rect(RenderSize);
            dc.DrawRoundedRectangle(WheelPainter.Solid(p.Hub), WheelPainter.Pen(p.Edge, 1), bounds, 6, 6);

            double radius = Math.Min(57, Math.Min((RenderSize.Width - 16) / 2, (RenderSize.Height - 12) / 2));
            if (radius < 20) return;
            var center = new Point(RenderSize.Width / 2, RenderSize.Height / 2);
            double inner = radius * 0.41;
            double ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var labels = new[] { "PDF", "IMG", "ZIP", "CUT", "TXT", "MP3", "DOC", "MORE" };
            var glyphs = new[] { "\uE8A5", "\uE91B", "\uE8B7", "\uE8C6", "\uE8A4", "\uE189", "\uE8A5", "\uE712" };

            dc.DrawEllipse(p.RingBrush, p.EdgePen, center, radius, radius);
            for (int i = 0; i < labels.Length; i++)
            {
                double start = -90 + i * 45;
                var segment = WheelPainter.AnnularSector(center, inner + 1, radius - 1, start + 0.6, 43.8);
                if (i == 1)
                    dc.DrawGeometry(WheelPainter.Solid(ColorUtil.Mix(p.Ring, p.Convert, p.IsLight ? 0.14 : 0.25)),
                        WheelPainter.Pen(ColorUtil.WithAlpha(p.Convert, (byte)0xA0), 0.8), segment);
                else
                    dc.DrawGeometry(p.RingBrush, null, segment);

                double angle = (start + 22.5) * Math.PI / 180;
                double textRadius = (inner + radius) / 2;
                double x = center.X + Math.Cos(angle) * textRadius;
                double y = center.Y + Math.Sin(angle) * textRadius;
                var ink = i == 1 ? WheelPainter.Solid(p.Convert) : p.TextMutedBrush;
                var icon = WheelPainter.Text(glyphs[i], WheelPainter.IconFace, 7.5, ink, ppd);
                dc.DrawText(icon, new Point(x - icon.Width / 2, y - 9));
                var label = WheelPainter.Text(labels[i], WheelPainter.Face(null, FontWeights.SemiBold), 5.3, p.TextBrush, ppd);
                dc.DrawText(label, new Point(x - label.Width / 2, y + 0.3));
            }

            dc.DrawEllipse(p.HubBrush, WheelPainter.Pen(p.Convert, 1.2), center, inner - 1.2, inner - 1.2);
            dc.DrawEllipse(WheelPainter.Solid(p.Convert), null, new Point(center.X, center.Y - 8.5), 1.7, 1.7);
            var title = WheelPainter.Text("ORBIT", WheelPainter.Face(null, FontWeights.Bold), 7.1, p.TextBrush, ppd);
            dc.DrawText(title, new Point(center.X - title.Width / 2, center.Y - 4.5));
            var subtitle = WheelPainter.Text("actions", WheelPainter.Face(null, FontWeights.Normal), 5.5, p.TextMutedBrush, ppd);
            dc.DrawText(subtitle, new Point(center.X - subtitle.Width / 2, center.Y + 5));
        }
    }
}
