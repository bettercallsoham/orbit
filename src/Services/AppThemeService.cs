using System;
using System.Windows;
using System.Windows.Media;
using Orbit.Wheel;
using Orbit.Models;

namespace Orbit.Services
{
    /// <summary>Three palettes shared by the windows and the drawn wheel.</summary>
    public static class AppThemeService
    {
        public sealed record Palette(string Name, string Background, string Surface, string Elevated,
            string Accent, string Highlight, string Text, string Secondary, string Border, bool IsLight);

        public static readonly Palette Eclipse = new("Eclipse", "#0D0B14", "#191522", "#292334", "#A78BFA", "#D8CCFF", "#F5F1FF", "#B6AFC8", "#393244", false);
        public static readonly Palette Boreal = new("Boreal", "#0B1718", "#142526", "#203536", "#55D6BE", "#A4F0DF", "#E8FAF7", "#A6C5C0", "#304748", false);
        public static readonly Palette Solstice = new("Solstice", "#F7F3EB", "#FFFFFF", "#EDE9F9", "#5B4BDB", "#DCD5FF", "#28243A", "#6B6577", "#DCD6CB", true);

        public static Palette Current { get; private set; } = Eclipse;
        public static bool IsLight => Current.IsLight;
        public static event Action? Changed;
        public static string Normalize(string? name) => name is "Boreal" or "Solstice" ? name : "Eclipse";

        public static void Apply(string? name)
        {
            Current = Normalize(name) switch { "Boreal" => Boreal, "Solstice" => Solstice, _ => Eclipse };
            var res = Application.Current?.Resources;
            if (res == null) return;
            var p = Current;
            Color C(string value) => ColorUtil.Parse(value, Colors.Magenta);
            void Brush(string key, Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                res[key] = brush;
            }

            var background = C(p.Background);
            var surface = C(p.Surface);
            var elevated = C(p.Elevated);
            var accent = C(p.Accent);
            var highlight = C(p.Highlight);
            var primary = C(p.Text);
            var secondary = C(p.Secondary);
            var border = C(p.Border);
            var contrast = p.IsLight ? Colors.Black : Colors.White;
            Brush("WindowBrush", background);
            Brush("SurfaceBrush", surface);
            Brush("SurfaceRaisedBrush", elevated);
            Brush("SurfaceHoverBrush", ColorUtil.Mix(elevated, primary, p.IsLight ? 0.055 : 0.07));
            Brush("SurfacePressedBrush", ColorUtil.Mix(elevated, primary, p.IsLight ? 0.11 : 0.13));
            Brush("SelectedSurfaceBrush", ColorUtil.Mix(elevated, accent, p.IsLight ? 0.1 : 0.16));
            Brush("ChromeBrush", surface);
            Brush("InputBrush", surface);
            Brush("BorderBrush", border);
            Brush("BorderStrongBrush", ColorUtil.Mix(border, secondary, 0.28));
            Brush("DividerBrush", border);
            Brush("TextPrimaryBrush", primary);
            Brush("TextSecondaryBrush", secondary);
            Brush("TextTertiaryBrush", ColorUtil.Mix(secondary, p.IsLight ? background : elevated, 0.13));
            Brush("DisabledTextBrush", ColorUtil.Mix(secondary, background, p.IsLight ? 0.42 : 0.4));
            res["AccentColor"] = accent;
            Brush("AccentBrush", accent);
            Brush("AccentHoverBrush", p.IsLight ? ColorUtil.Mix(accent, Colors.Black, 0.12) : highlight);
            Brush("AccentPressedBrush", ColorUtil.Mix(accent, Colors.Black, p.IsLight ? 0.2 : 0.14));
            Brush("FocusBrush", accent);
            Brush("SelectionBrush", ColorUtil.WithAlpha(accent, (byte)(p.IsLight ? 0x38 : 0x65)));
            Brush("AccentSubtleBrush", ColorUtil.WithAlpha(accent, (byte)0x24));
            Brush("AccentFaintBrush", ColorUtil.WithAlpha(accent, (byte)0x16));
            Brush("OnAccentBrush", p.IsLight ? Colors.White : background);
            foreach (string mode in new[] { "Convert", "Tools", "Actions" })
            {
                Brush($"Mode{mode}Brush", accent);
                Brush($"Mode{mode}SubtleBrush", ColorUtil.WithAlpha(accent, (byte)0x24));
            }
            Color success = C(p.IsLight ? "#187B53" : "#69D8AC");
            Color warning = C(p.IsLight ? "#946010" : "#F0CB79");
            Color danger = C(p.IsLight ? "#B83345" : "#F283A1");
            Brush("SuccessBrush", success);
            Brush("WarningBrush", warning);
            Brush("DangerBrush", danger);
            Brush("SuccessSubtleBrush", ColorUtil.WithAlpha(success, (byte)0x22));
            Brush("WarningSubtleBrush", ColorUtil.WithAlpha(warning, (byte)0x22));
            Brush("DangerSubtleBrush", ColorUtil.WithAlpha(danger, (byte)0x22));
            Brush("HoverTintBrush", ColorUtil.WithAlpha(contrast, (byte)(p.IsLight ? 0x0C : 0x1A)));
            Brush("PressTintBrush", ColorUtil.WithAlpha(contrast, (byte)(p.IsLight ? 0x18 : 0x10)));
            Brush("DropZoneBrush", ColorUtil.WithAlpha(contrast, (byte)0x08));
            Brush("ScrollThumbBrush", ColorUtil.Mix(border, secondary, 0.18));
            Brush("ScrollThumbHoverBrush", ColorUtil.Mix(border, secondary, 0.42));
            Brush("PopupBrush", surface);
            res["BrandStartColor"] = accent;
            res["BrandEndColor"] = p.IsLight ? ColorUtil.Mix(accent, Colors.Black, 0.1) : highlight;
            res["CardRadius"] = new CornerRadius(12);
            res["ControlRadius"] = new CornerRadius(8);
            Changed?.Invoke();
        }

        // Preserve the template-facing API used by existing verification and integrations.
        public static void Apply(WheelTemplate template) => Apply(Current.Name);
    }
}
