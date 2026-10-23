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
                    Ring = "#292334", Hub = "#191522", Text = "#F5F1FF",
                    TextMuted = "#B6AFC8", TextFaint = "#B6AFC8",
                    Edge = "#393244", Divider = "#393244", Shadow = "#00000000",
                    Convert = "#A78BFA", Tools = "#A78BFA", Actions = "#A78BFA",
                    OnAccent = "#0D0B14"
                },
                App = new AppColors
                {
                    Base = "dark", Window = "#0D0B14", Surface = "#191522",
                    SurfaceRaised = "#292334", SurfaceHover = "#332C40", SurfacePressed = "#3D3351",
                    Input = "#191522", Border = "#393244", BorderStrong = "#5C536B",
                    Divider = "#393244", TextPrimary = "#F5F1FF", TextSecondary = "#B6AFC8",
                    TextTertiary = "#A09AB1", Accent = "#A78BFA",
                    Success = "#69D8AC", Warning = "#F0CB79", Danger = "#F283A1",
                    CardRadius = 12, ControlRadius = 8
                },
                Shape = new WheelShape { InnerRatio = 0.39 },
                Motion = new WheelMotion { Open = "fade", Switch = "fade", DurationMs = 160, HoverMs = 70 },
                Sounds = new WheelSounds { Pack = "soft" }
            }
        };
    }
}
