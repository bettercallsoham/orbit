using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Orbit.Services
{
    public class TrayIconService : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 101;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_ENDSESSION = 0x0016;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint MF_STRING = 0;
        private const uint MF_SEPARATOR = 0x0800;
        private const int OPEN_COMMAND = 1;
        private const int EXIT_COMMAND = 2;
        private const int HWND_BROADCAST = 0xffff;

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string? text);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr owner, IntPtr rect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr menu);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        private const int SM_CXSMICON = 49;

        // Sent to all top-level windows when Explorer (and the taskbar) restarts; tray icons must be re-added
        private static readonly int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");
        private static readonly int WM_OPEN_ORBIT = RegisterWindowMessage("Orbit.OpenMainWindow");

        public static void RequestExistingWindow() => PostMessage(new IntPtr(HWND_BROADCAST), WM_OPEN_ORBIT, IntPtr.Zero, IntPtr.Zero);

        private IntPtr _hwnd;
        private HwndSource? _hwndSource;
        private bool _isCreated;
        private IntPtr _hIcon;
        private bool _ownsIcon;

        public event Action? TrayClicked;
        public event Action? ExitRequested;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.Handle;

            if (_hwnd == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) =>
                {
                    _hwnd = new WindowInteropHelper(window).Handle;
                    SetupHook();
                    CreateIcon();
                };
            }
            else
            {
                SetupHook();
                CreateIcon();
            }
        }

        private void SetupHook()
        {
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);
        }

        private void CreateIcon()
        {
            if (_hwnd == IntPtr.Zero || _isCreated) return;

            if (_hIcon == IntPtr.Zero) _hIcon = LoadAppIcon(out _ownsIcon);

            var data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "Orbit"
            };

            _isCreated = Shell_NotifyIcon(NIM_ADD, ref data);
        }

        /// <summary>
        /// Loads the app icon at the tray's small-icon size for the window's DPI (16px at 100%, 24px at 150%, ...),
        /// so Windows picks the crisp hand-made frame instead of scaling a large one down.
        /// </summary>
        private IntPtr LoadAppIcon(out bool owned)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "resources", "Brand", "orbit.ico");
            if (!File.Exists(path)) path = ExtractEmbeddedIcon() ?? path;
            if (File.Exists(path))
            {
                uint dpi = GetDpiForWindow(_hwnd);
                int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi == 0 ? 96 : dpi);
                IntPtr icon = LoadImage(IntPtr.Zero, path, IMAGE_ICON, size, size, LR_LOADFROMFILE);
                if (icon != IntPtr.Zero)
                {
                    owned = true;
                    return icon;
                }
            }

            // Fallback: the generic Windows application icon (shared, must not be destroyed)
            owned = false;
            const int IDI_APPLICATION = 32512;
            return LoadIcon(IntPtr.Zero, new IntPtr(IDI_APPLICATION));
        }

        /// <summary>
        /// Single-file builds bundle the loose .ico inside the exe, but LoadImage needs a real file:
        /// write the embedded copy to the user data folder once and load it from there.
        /// </summary>
        private static string? ExtractEmbeddedIcon()
        {
            try
            {
                string target = Path.Combine(AppPaths.UserDataDirectory, "orbit-brand.ico");
                var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/resources/Brand/orbit.ico"));
                if (resource == null) return null;
                using var source = resource.Stream;
                using var iconData = new MemoryStream();
                source.CopyTo(iconData);
                byte[] bytes = iconData.ToArray();
                if (!File.Exists(target) || !File.ReadAllBytes(target).AsSpan().SequenceEqual(bytes))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.WriteAllBytes(target, bytes);
                }
                return target;
            }
            catch
            {
                return null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Restart Manager uses this during upgrades; let Orbit release its hooks and exe cleanly.
            if (msg == WM_ENDSESSION && wParam != IntPtr.Zero)
            {
                ExitRequested?.Invoke();
                return IntPtr.Zero;
            }

            if (msg == WM_OPEN_ORBIT && WM_OPEN_ORBIT != 0)
            {
                TrayClicked?.Invoke();
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WM_TASKBARCREATED && WM_TASKBARCREATED != 0)
            {
                _isCreated = false;
                CreateIcon();
                return IntPtr.Zero;
            }

            if (msg == WM_TRAYICON)
            {
                int mouseMsg = (int)lParam;
                if (mouseMsg == WM_LBUTTONUP)
                {
                    TrayClicked?.Invoke();
                    handled = true;
                }
                else if (mouseMsg == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void ShowContextMenu()
        {
            IntPtr menu = CreatePopupMenu();
            if (menu == IntPtr.Zero) return;
            try
            {
                AppendMenu(menu, MF_STRING, OPEN_COMMAND, "Open Orbit");
                AppendMenu(menu, MF_SEPARATOR, 0, null);
                AppendMenu(menu, MF_STRING, EXIT_COMMAND, "Exit");
                if (!GetCursorPos(out POINT cursor)) return;
                SetForegroundWindow(_hwnd);
                int command = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, cursor.X, cursor.Y, 0, _hwnd, IntPtr.Zero);
                PostMessage(_hwnd, 0, IntPtr.Zero, IntPtr.Zero);
                if (command == OPEN_COMMAND) TrayClicked?.Invoke();
                else if (command == EXIT_COMMAND) ExitRequested?.Invoke();
            }
            finally
            {
                DestroyMenu(menu);
            }
        }

        public void RemoveIcon()
        {
            if (_isCreated && _hwnd != IntPtr.Zero)
            {
                var data = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hwnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref data);
                _isCreated = false;
            }
        }

        public void Dispose()
        {
            RemoveIcon();
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource = null;

            if (_ownsIcon && _hIcon != IntPtr.Zero) DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
