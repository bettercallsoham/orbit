using System.Collections.Generic;
using Orbit.Models;

namespace Orbit.Services
{
    /// <summary>The single built-in Orbit wheel template.</summary>
    internal static class BuiltInTemplates
    {
        public const string DefaultId = "orbit-current";

        public static List<WheelTemplate> Create() => new()
        {
            new WheelTemplate
            {
                Id = DefaultId,
                Name = "Orbit Current",
                Author = "Orbit",
                Description = "A precise, high contrast radial interface.",
                Renderer = "ring",
                Wheel = new WheelColors
                {
                    Ring = "#FF15161A", Hub = "#FF000000", Text = "#F4F4F6",
                    TextMuted = "#BFC0C7", TextFaint = "#91939D",
                    Edge = "#FF52057B", Divider = "#FF42434B", Shadow = "#00000000",
                    Convert = "#892CDC", Tools = "#892CDC", Actions = "#892CDC",
                    OnAccent = "#FFFFFF"
                },
                App = new AppColors
                {
                    Base = "dark", Window = "#000000", Surface = "#111215",
                    SurfaceRaised = "#1A1B20", SurfaceHover = "#24252C", SurfacePressed = "#2B2C33",
                    Input = "#0D0E11", Border = "#303139", BorderStrong = "#42434B",
                    Divider = "#26272D", TextPrimary = "#F4F4F6", TextSecondary = "#C2C3CA",
                    TextTertiary = "#9698A2", Accent = "#892CDC",
                    Success = "#69D8AC", Warning = "#F0CB79", Danger = "#F283A1",
                    CardRadius = 2, ControlRadius = 2
                },
                Shape = new WheelShape { InnerRatio = 0.39 },
                Motion = new WheelMotion { Open = "fade", Switch = "fade", DurationMs = 160, HoverMs = 70 },
                Sounds = new WheelSounds { Pack = "soft" }
            }
        };
    }
}
