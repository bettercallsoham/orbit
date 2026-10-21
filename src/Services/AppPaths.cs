using System;
using System.IO;

namespace Orbit.Services
{
    /// <summary>Application data locations for Orbit settings, templates and tools.</summary>
    internal static class AppPaths
    {
        public static string UserDataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Orbit");

        public static string LocalDataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orbit");
    }
}
